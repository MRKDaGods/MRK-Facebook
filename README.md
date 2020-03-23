# MRK-Facebook
#(CODE IS MESSY)
I have only open sourced the C# part, or you can call that the frontend of the project, due to security purposes obviously

I coded this mainly for informational purposes and to learn more about security
The hacked account is one of my aliases
I have been having a concept in my mind for a while now, and I finally executed it
Workflow:
the program mainly exploits a racing condition in Facebook's servers when entering the recovery code to reset X's account
The profile link is first entered and is then parsed to extract either the profile ID or profile username
Then the rest is handled by the WebBrowser component
Navigation to the target's profile is first done, account name, nickname, ID and profile picture are then extracted using HTML parsing
A fake login attempt to the target's account is then initiated using the extracted account ID and a random password
Facebook might then ask for an Email/Phone number which would be easily known for most people, sometimes it would not ask for that; verification purposes.
Facebook then creates a forgot password context for the account and then I start a multithreaded smart bruteforcing for the recovery code using alternating proxies for every 5 attempts
The code is eventually found, and then a new password is set
and done
