This project is solano sniper bot which is made in c# and based on console application
# Note this bot swap token system doesn't work right now because the cancelation of legacy transaction on jupiter api and also the fact that none of c# packages can handel versioned transaction in this time :C

How to use this bot?

1.Enter your 12 word seed phrase from phantom wallet 

2.Input the amount of sol you want to spend input in lamports format you can covert sol to lamports here https://www.solconverter.com/

3.Enter your helius api key https://www.helius.dev/

4.Enjoy snipping memecoins 😎😎😎


Also you can use this sorce code to make discord bot or telegram bot if you want and the time that this bot uses to detect new token is around 15 sec after migration but it's still up to the rpc server speed
Also If you don't want to snipe pump.fun migrated token you can change Program id in Program class line 75 to something else like Raydium program id or pump.fun normal token 
but you also need to change Program log in Program class line 101 to log that can be used to detect newly created token from your intended program id

And you can add more setting from jupiter search api  
which you can put in tokenroot class and add it to the checktoken method 

# This project is not done yet but it's pretty much usable right now only that it can't perform auto buy but you can just buy the token from the gmgn link that's given by this bot anyway
