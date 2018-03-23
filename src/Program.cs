using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net;
using GatewayApiClient;
using GatewayApiClient.DataContracts;
using GatewayApiClient.DataContracts.EnumTypes;
using GatewayApiClient.EnumTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StoneGatewayPoc
{
    class Program
    {
        static void Main(string[] args)
        {
            Guid merchantKey = Guid.Parse(ConfigurationManager.AppSettings["merchantKey"]);
            var client = new GatewayServiceClient(merchantKey, new Uri("https://transaction.stone.com.br"));

            #region help with brands
            /*
             | Bandeira   | Comeca com                                  | Máximo de número | Máximo de número cvc |
             | ---------- | ------------------------------------------- | ---------------- | -------------------- |
             | Visa       | 4                                           | 13,16            | 3                    |
             | Mastercard | 5                                           | 16               | 3                    |
             | Diners     | 301,305,36,38                               | 14,16            | 3                    |
             | Elo        | 636368,438935,504175,451416,509048,509067,  |                  | 3(?)
             |            | 509049,509069,509050,509074,509068,509040,
             |            | 509045,509051,509046,509066,509047,509042,
             |            | 509052,509043,509064,509040                 |                  |                      
             |            | 36297, 5067,4576,4011                       | 16               | 3
             | Amex       | 34,37                                       | 15               | 4                    |
             | Discover   | 6011,622,64,65                              | 16               | 4                    |
             | Aura       | 50                                          | 16               | 3                    |
             | jcb        | 35                                          | 16               | 3                    |
             | Hipercard  | 38,60                                       | 13,16,19         | 3                    |
             */
            #endregion

            var begin = 7;
            Console.SetCursorPosition(1, 1);
            Console.WriteLine("choice a option:");
            Console.SetCursorPosition(1, 2);
            Console.WriteLine("1 - to create a InstanBuyKey");
            Console.SetCursorPosition(1, 3);
            Console.WriteLine("2 - to find a credit card");
            Console.SetCursorPosition(1, 4);
            Console.WriteLine("3 - to create a transaction fake");
            Console.SetCursorPosition(1, 5);
            Console.WriteLine("4 - to cancel a transaction");
            Console.SetCursorPosition(1, begin);
            var readKey = Console.Read();
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };
            switch (readKey)
            {
                case 49: //key 1
                    var createInstantBuyDataRequest = new CreateInstantBuyDataRequest()
                    {
                        CreditCardBrand = CreditCardBrandEnum.Mastercard,
                        CreditCardNumber = "1111222233334444",
                        ExpMonth = 01,
                        ExpYear = 20,
                        HolderName = "John White".ToUpper(),
                        IsOneDollarAuthEnabled = false,
                        SecurityCode = "123"
                    };
                    var createCreditCardResponse = client.CreditCard.CreateCreditCard(createInstantBuyDataRequest);
                    if (createCreditCardResponse.HttpStatusCode == HttpStatusCode.Created && createCreditCardResponse.Response.Success == true)
                    {
                        Console.SetCursorPosition(1, begin);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(1, begin);
                        Console.WriteLine("InstantBuy Key: {0}", createCreditCardResponse.Response.InstantBuyKey);
                    }
                    break;
                case 50: //key 2
                    var instantBuyKey = Guid.Parse(ConfigurationManager.AppSettings["instantBuyKey"]);
                    var getCreditCardResponse = client.CreditCard.GetCreditCard(instantBuyKey);
                    var result = getCreditCardResponse.RawResponse;
                    
                    var obj = JsonConvert.DeserializeObject(result);
                    var json = JsonConvert.SerializeObject(obj, serializerSettings);
                    //
                    Console.SetCursorPosition(1, begin);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(1, begin);
                    Console.WriteLine(json);
                    break;
                case 51: //key 3
                    var value = 86.21100000000001;
                    var transaction = new CreditCardTransaction()
                    {
                        AmountInCents = (long)(value * 100),
                        InstallmentCount = 1,
                        //this param is simulation payment
                        Options = new CreditCardTransactionOptions { PaymentMethodCode = 1 },
                        CreditCardOperation = CreditCardOperationEnum.AuthAndCapture,
                        CreditCard = new CreditCard()
                        {
                            InstantBuyKey = Guid.Parse(ConfigurationManager.AppSettings["instantBuyKey"])
                        }
                    };
                    var createSaleRequest = new CreateSaleRequest()
                    {
                        CreditCardTransactionCollection = new Collection<CreditCardTransaction>(new CreditCardTransaction[] { transaction }),
                        Order = new Order { OrderReference = Guid.NewGuid().ToString() }
                    };
                    var serviceClient = new GatewayServiceClient();
                    var httpResponse = serviceClient.Sale.Create(createSaleRequest);
                    var jsonResponse = JsonConvert.SerializeObject(httpResponse, serializerSettings);
                    //
                    Console.SetCursorPosition(1, begin);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(1, begin);
                    Console.WriteLine(jsonResponse);
                    break;
                case 52: //key 4
                    var orderKey = Guid.Parse("094c775d-ea43-429f-97b9-4474c3fdqwe");
                    var saleManageResponse = client.Sale.Manage(ManageOperationEnum.Cancel, orderKey);

                    if (saleManageResponse.HttpStatusCode == HttpStatusCode.OK
                            && saleManageResponse.Response.CreditCardTransactionResultCollection.Any()
                            && saleManageResponse.Response.CreditCardTransactionResultCollection.All(p => p.Success == true))
                    {
                        var respManage = client.Sale.Manage(ManageOperationEnum.Cancel, orderKey).RawResponse;
                        var objRespManage = JsonConvert.DeserializeObject(respManage);
                        var jsonRespManage = JsonConvert.SerializeObject(objRespManage, serializerSettings);
                        Console.SetCursorPosition(1, begin);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(1, begin);
                        Console.WriteLine(jsonRespManage);
                    }
                    break;
                default:
                    Console.WriteLine("Not found key!");
                    break;
            }
            //
            Console.ReadKey();
        }
    }
}