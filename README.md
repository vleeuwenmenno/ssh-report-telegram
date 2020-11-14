# A simple script to confirm SSH login with Telegram

I am not sure how secure it is but you can decide that yourself.


### How to install

1. Download the binary from (releases)[https://github.com/vleeuwenmenno/ssh-report-telegram/releases]
    ### For arm based hosts (Raspberry Pi)
    - ```wget https://github.com/vleeuwenmenno/ssh-report-telegram/releases/download/v1.1/ssh-report-arm -O ssh-report```
    ### For Intel/AMD based hosts
    - ```wget https://github.com/vleeuwenmenno/ssh-report-telegram/releases/download/v1.1/ssh-report-amd64 -O ssh-report```

2. Allow execution to the file
    - ```chmod +x ssh-report```
    
3. Allow execution to the file
    - ```sudo ./ssh-report install```

4. Enter your bot chat id
5. Enter your bot API token
6. Try logging in

## Spam default welcome message?

You can disable the Debian message with

    sudo chmod -x /etc/update-motd.d/*

And sometimes you also need to do

    touch ~/.hushlogin

## Custom welcome message from ssh-report

You can change `/etc/profile` here you can update the line right after `if [ "$?" -eq "40" ]; then`
<br/>Just remove `/usr/bin/ssh-report welcome` and add your own script there.

## Don't know how to get the chat id/bot token?

Have a look at this StackOverflow

https://stackoverflow.com/questions/32423837/telegram-bot-how-to-get-a-group-chat-id#:~:text=Go%20to%20the%20group%2C%20click,dummy%20message%20to%20the%20bot.&text=4%2D%20Look%20for%20%22chat%22,(with%20the%20negative%20sign).