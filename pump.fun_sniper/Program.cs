using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using WebSocketSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace pump.fun_sniper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("   _____  ____  _          _____ _   _ _____ _____  ______ _____    \r\n  / ____|/ __ \\| |        / ____| \\ | |_   _|  __ \\|  ____|  __ \\   \r\n | (___ | |  | | |       | (___ |  \\| | | | | |__) | |__  | |__) |  \r\n  \\___ \\| |  | | |        \\___ \\| . ` | | | |  ___/|  __| |  _  /   \r\n  ____) | |__| | |____    ____) | |\\  |_| |_| |    | |____| | \\ \\   \r\n |_____/ \\____/|______|  |_____/|_| \\_|_____|_|    |______|_|  \\_\\  \r\n                                                                    \r\n                                                          Made by Erza_x   ");

            Console.WriteLine("Enter your 12 word seed phrase from phantom wallet in this format also you need minimum of 10 usd in your phantom wallet");
            Console.WriteLine("see1 seed2 seed3 seed4 ...... seed12");
            string seed = Console.ReadLine();
            Dependencies.seedPhrase = seed;
            getbalance();
            Console.WriteLine("The amount of sol you want to spend input in lamports format you can covert sol to lamports here https://www.solconverter.com/");
            string amount = Console.ReadLine();
            Dependencies.amount = amount;
            Console.WriteLine("Enter your helius api key https://www.helius.dev/");
            string key = Console.ReadLine();
            Dependencies.heliusapi_key = key;
            start();
            Console.ReadLine();
        }

        static void getbalance()
        {
            var rpc = ClientFactory.GetClient(Cluster.MainNet);
            var myWallet = new Wallet(Dependencies.seedPhrase);
            var from = myWallet.GetAccount(0);
            string publickey = from.PublicKey;

            var bal = rpc.GetBalance(from.PublicKey);
            int lamports = (int)bal.Result.Value;
            decimal sol = lamportstosol(lamports);
            Console.WriteLine($"Balance: {sol} SOL");
        }
        static decimal lamportstosol(int lamports)
        {
            decimal sol = lamports / 1000000000m;
            return sol;
        }

        static void start()
        {
            try
            {
                using WebSocket ws = new WebSocket($"wss://mainnet.helius-rpc.com/?api-key={Dependencies.heliusapi_key}"); 
                {
                    ws.OnOpen += (s, e) =>
                    {
                        Console.WriteLine("WebSocket Connected");

                        var request = new
                        {
                            jsonrpc = "2.0",
                            id = 1,
                            method = "logsSubscribe",
                            @params = new object[]
                        {
                             new
                             {
                               mentions = new [] { "6EF8rrecthR5Dkzon8Nwu78hRvfCKubJ14M5uBEwF6P" }
                             },
                            new
                            {
                                 commitment = "finalized"
                            }
                            }
                        };

                        string json = JsonConvert.SerializeObject(request);
                        ws.Send(json);
                    };



                    ws.OnMessage += (sender, e) =>
                    {
                        JObject json = JObject.Parse(e.Data);
                        var logs = json["params"]?["result"]?["value"]?["logs"];
                        var signature = json["params"]?["result"]?["value"]?["signature"];

                        if (logs != null || signature != null)
                        {
                          
                            Console.WriteLine("<Finding token>");

                            if (logs.Any(logs => logs.ToString().Contains("Program log: Instruction: CreatePool")))
                            {
                                ws.Close();
                                Console.WriteLine("Found new liquidity pool");  // signature example = 28S3N2XBPw1ZVSbRcbv6cJqkVSCrEZvoqyyRRvNGcuqDXoB9m72CRHucPkwewcJamfjU7umbEqNqE9JPwZYC5GF9
                                getmint(signature.ToString()).Wait();
                            }
                        }


                    };
                    ws.OnError += (sender, e) =>
                    {
                        Console.WriteLine("WebSocket ERROR: " + e.Message);
                    };

                    ws.OnClose += (sender, e) =>
                    {
                        Console.WriteLine("WebSocket closed");
                    };

                    ws.Connect();
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        static async Task getmint(string signature)
        {
            try
            {
                string url = $"https://api-mainnet.helius-rpc.com/v0/transactions/?api-key={Dependencies.heliusapi_key}";
                using (var client = new HttpClient())
                {
                    var requestBody = new
                    {
                        transactions = new[]
                    {
                        $"{signature}"
                    }
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var parsed = JArray.Parse(responseString);

                    string pumpfunMint = null;

                    foreach (var tx in parsed)
                    {
                        var tokenTransfers = tx?["tokenTransfers"]?.ToArray();
                        if (tokenTransfers == null) continue;

                        foreach (var transfer in tokenTransfers)
                        {
                            string mint = transfer?["mint"]?.ToString() ?? "";

                            if (mint.Contains("pump"))
                            {
                                pumpfunMint = mint;
                                break;
                            }
                        }
                    }
                    checktoken(pumpfunMint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void checktoken(string mint)
        {
            string url = $"https://lite-api.jup.ag/ultra/v1/search?query={mint}";
            Checking_config config = new Checking_config();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", "b31802c9-5697-4ecc-bb50-42a7b10ae5f2");
                try
                {
                    var json = client.GetStringAsync(url).Result;
                    var tokenroots = JsonConvert.DeserializeObject<List<tokenroot>>(json);
                    if(tokenroots[0].mcap >= config.avg_mcap && tokenroots[0].holderCount >= config.avg_holders && tokenroots[0].stats5M != null && tokenroots[0].stats5M.numBuys >= config.avg_buys && tokenroots[0].stats5M.numSells >= config.avg_sells &&
                        tokenroots[0].stats5M.numBuys > tokenroots[0].stats5M.numSells )
                    {

                        var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                        DateTime thaiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, thaiTimeZone);

                        // Print in hour:minute:second
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("This token is good proceed to swap" );
                        Console.WriteLine("========================================");
                        Console.WriteLine("The token name is = " + tokenroots[0].name);
                        Console.WriteLine("The market cap is = " + tokenroots[0].mcap);
                        Console.WriteLine("The holder count is = " + tokenroots[0].holderCount);
                        Console.WriteLine("The buys in last 5 min are = " + tokenroots[0].stats5M.numBuys);
                        Console.WriteLine("The sells in last 5 min are = " + tokenroots[0].stats5M.numSells);
                        Console.WriteLine("Found this token at " + thaiTime.ToString("HH:mm:ss"));
                        Console.WriteLine($"https://gmgn.ai/sol/token/{mint}");
                        Console.WriteLine("========================================");
                        Console.ForegroundColor = Console.ForegroundColor;
                        // Swap_Token(mint).Wait(); doesn't work properly yet
                    }
                    else
                    {
                        Console.WriteLine("This token is bad");
                        start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

        }
        public class tokenroot 
        {
            public string name { get; set; }
            public int holderCount { get; set; }
            public double mcap { get; set; }
            public stats5m stats5M { get; set; }

        }

        public class stats5m
        {
            public int numBuys { get; set; }
            public int numSells { get; set; }
        }


        static async Task Swap_Token(string mint)
        {
            var myWallet = new Wallet(Dependencies.seedPhrase);
            var account = myWallet.GetAccount(0);

            var qurl = new HttpRequestMessage(HttpMethod.Get, "https://api.jup.ag/swap/v1/quote?" +
                 "slippageBps=50" +
                 "&swapMode=ExactIn" +
                 "&restrictIntermediateTokens=true" +
                 "&maxAccounts=30" +
                 "&asLegacyTransaction=true" +
                 "&inputMint=So11111111111111111111111111111111111111112" +
                 $"&outputMint={mint}" +
                 $"&amount={Dependencies.amount}");

            using (var client = new HttpClient())
            {
                try
                {

                    // 1st: Get Quote
                    client.DefaultRequestHeaders.Add("x-api-key", "b31802c9-5697-4ecc-bb50-42a7b10ae5f2");
                    var response = await client.SendAsync(qurl);
                    var json = await response.Content.ReadAsStringAsync();

                    // #Check for quote errors
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Quote Error: " + json);
                        return;
                    }

                    var quoteResponse = JsonSerializer.Deserialize<JsonElement>(json);

                    // 2.nd: build Swap requestBody
                    var requestBody = new
                    {
                        userPublicKey = account.PublicKey.ToString(),
                        quoteResponse = quoteResponse,
                        dynamicComputeUnitLimit = true,
                        prioritizationFeeLamports = new
                        {
                            priorityLevelWithMaxLamports = new
                            {
                                maxLamports = 1000000,
                                priorityLevel = "veryHigh"
                            }
                        }
                    };

                    var swpjson = JsonSerializer.Serialize(requestBody);
                    var swpcontent = new StringContent(swpjson, Encoding.UTF8, "application/json");


                    // 3.rd: Send Swap request
                    var swpresponse = await client.PostAsync("https://api.jup.ag/swap/v1/swap", swpcontent);
                    var result = await swpresponse.Content.ReadAsStringAsync();

                    // #Check for swap errors
                    if (!swpresponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Swap Error: " + result);
                        return;
                    }

                    var swapResponse = JsonSerializer.Deserialize<JsonElement>(result);

                    // #Check if swapTransaction exists
                    if (!swapResponse.TryGetProperty("swapTransaction", out JsonElement swapTxElement))
                    {
                        Console.WriteLine("Error: No swapTransaction in response: " + result);
                        return;
                    }

                    var swapTransaction = swapTxElement.GetString();
                    var txBytes = Convert.FromBase64String(swapTransaction);

                    // 4.th: Sign transaction
                    var legacyTx = Transaction.Deserialize(txBytes);
                    legacyTx.Sign(account);
                    var signedTx = Convert.ToBase64String(legacyTx.Serialize());


                    // 5.th: Send transaction to Solana network 
                    var sendTxRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.mainnet-beta.solana.com");
                    sendTxRequest.Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            jsonrpc = "2.0",
                            id = 1,
                            method = "sendTransaction",
                            @params = new object[]
                            {
                               signedTx,
                               new
                               {
                                  encoding = "base64",
                                  skipPreflight = true,
                                  maxRetries = 2
                               }
                            }
                        }),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var sendTxResponse = await client.SendAsync(sendTxRequest);
                    var sendTxResult = await sendTxResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(sendTxResult);
                    var signature = txResponse.GetProperty("result").GetString();

                    // 6.th: Output results
                    Console.WriteLine("Status Code: " + sendTxResponse.StatusCode);
                    Console.WriteLine("Full Response: " + sendTxResult);


                    // #Check if there's Output error
                    if (txResponse.TryGetProperty("error", out var error))
                    {
                        Console.WriteLine("Error: " + error.ToString());
                    }
                    else if (txResponse.TryGetProperty("result", out var results))
                    {
                        var signatures = results.GetString();
                        Console.WriteLine($"Signature: {signature}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }







        public class root()
        {
           public string transaction { get; set; }
           public string requestId { get; set; }
        }
    }
}
