# Twitch Bot
This bot is just a simple local application to connect to the Twitch chat and react to commands and messages.

You cannot compare this bot to [Moobot](https://moo.bot/) or other well-known bots. You need to run and configure this application by yourself.

# Purpose
This is just a small side project to help a friend. We needed a simple bot framework to add some custom command to the Twitch chat. We couldn't find any other bot that worked for us - so I did it myself.

# Why you shouldn't use this bot...
This is a very quick project without documentation. 

The moderation functions are limited. For example: you can simply bypass the link detection method by using spaces. Other bots provide more features and better moderation tools.

The Twitch authentication is also extremely dirty. The application creates a local web-server to emulate an OAuth page. You have to create your own Twitch App on localhost. This just bypasses the process of having to create and host a proper OAuth page.