﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ripple.TxSigning;
using RippleDotNet.Extensions;
using RippleDotNet.Json.Converters;
using RippleDotNet.Model;
using RippleDotNet.Model.Account;
using RippleDotNet.Model.Transaction;
using RippleDotNet.Model.Transaction.Interfaces;
using RippleDotNet.Model.Transaction.TransactionTypes;
using RippleDotNet.Requests.Ledger;
using RippleDotNet.Requests.Transaction;
using RippleDotNet.Responses.Transaction.Interfaces;

namespace RippleDotNet.Tests
{
    [TestClass]
    public class TransactionTests
    {
        private static IRippleClient client;
        private static JsonSerializerSettings serializerSettings;

        //private static string serverUrl = "wss://s.altnet.rippletest.net:51233";
        
        
        //private static string serverUrl = "wss://s1.ripple.com:443";
        private static string serverUrl = "wss://s2.ripple.com:443";

        public TestContext TestContext { get; set; }

        
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            client = new RippleClient(serverUrl);
            client.Connect();

            serializerSettings = new JsonSerializerSettings();
            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            serializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            serializerSettings.Converters.Add(new TransactionConverter());
        }


        [TestMethod]
        public async Task CanGetTransaction()
        {
            //transaction on mainnet
            ITransactionResponseCommon transaction = await client.Transaction("5FF261E0E463EF3CA9E2BD4F0754E398A3DBAADF71A3911190C5F9A1241ED403");
            Assert.IsNotNull(transaction);           
        }

        [TestMethod]
        public async Task CanGetTransactionAsBinary()
        {
            //transaction on mainnet
            IBaseTransactionResponse transaction = await client.TransactionAsBinary("5FF261E0E463EF3CA9E2BD4F0754E398A3DBAADF71A3911190C5F9A1241ED403");            
            Assert.IsNotNull(transaction);
        }

        [TestMethod]
        public async Task CanGetTransactions()
        {
            RippleClient rippleClient = new RippleClient("wss://s1.ripple.com:443");
            rippleClient.Connect();
            var transactions = await rippleClient.AccountTransactions("rPGKpTsgSaQiwLpEekVj1t5sgYJiqf2HDC");
            Console.WriteLine(transactions.Transactions.Count);
        }

        [TestMethod]
        public void CanSerializeAndDeserializeHex()
        {
            //https://ripple.com/build/transactions/#domain
            var domain = "example.com";
            var hex = domain.ToHex();
            Assert.AreEqual(0, string.Compare("6578616d706c652e636f6d", hex, StringComparison.OrdinalIgnoreCase));
            var result = hex.FromHexString();
            Assert.AreEqual(domain, result);
        }

        [TestMethod]
        public void CanCreatePaymentTransaction()
        {
            IPaymentTransaction paymentTransaction = new PaymentTransaction();
            paymentTransaction.Account = "rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V";
            paymentTransaction.Destination = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT";
            paymentTransaction.Amount = new Currency{ ValueAsXrp = 1 };

            const string expectedResult = "{\"Amount\":\"1000000\",\"Destination\":\"rEqtEHKbinqm18wQSQGstmqg9SFpUELasT\",\"Flags\":2147483648,\"Account\":\"rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V\",\"TransactionType\":\"Payment\"}";
            
            Assert.AreEqual(expectedResult, paymentTransaction.ToString());            
        }

        [TestMethod]
        public async Task CanSignAndSubmitPaymentTransaction()
        {
            IPaymentTransaction paymentTransaction = new PaymentTransaction();
            paymentTransaction.Account = "rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V";
            paymentTransaction.Destination = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT";
            paymentTransaction.Amount = new Currency { ValueAsXrp = 1 };

            SubmitRequest request = new SubmitRequest();
            request.Transaction = paymentTransaction;
            request.Offline = false;
            request.Secret = "xxxxxxx";

            Submit result = await client.SubmitTransaction(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("tesSUCCESS", result.EngineResult);            
            Assert.IsNotNull(result.Transaction.Hash);           
        }

        [TestMethod]
        public async Task CanSubmitPaymentTransaction()
        {
            AccountInfo accountInfo = await client.AccountInfo("rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V");

            IPaymentTransaction paymentTransaction = new PaymentTransaction();
            paymentTransaction.Account = "rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V";
            paymentTransaction.Destination = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT";
            paymentTransaction.Amount = new Currency { ValueAsXrp = 1};
            paymentTransaction.Sequence = accountInfo.AccountData.Sequence;

            var json = paymentTransaction.ToString();
            TxSigner signer = TxSigner.FromSecret("xxxxxxx");
            SignedTx signedTx = signer.SignJson(JObject.Parse(json));

            SubmitBlobRequest request = new SubmitBlobRequest();
            request.TransactionBlob = signedTx.TxBlob;

            Submit result = await client.SubmitTransactionBlob(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("tesSUCCESS", result.EngineResult);
            Assert.IsNotNull(result.Transaction.Hash);
        }

        [TestMethod]
        public async Task CanEstablishTrust()
        {
            AccountInfo accountInfo = await client.AccountInfo("rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V");

            ITrustSetTransaction trustSet = new TrustSetTransaction();
            trustSet.LimitAmount = new Currency{CurrencyCode = "XYZ", Issuer = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT", Value = "1000000"};
            trustSet.Account = "rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V";
            trustSet.Sequence = accountInfo.AccountData.Sequence;

            var json = trustSet.ToString();
            TxSigner signer = TxSigner.FromSecret("xxxxxxx");
            SignedTx signedTx = signer.SignJson(JObject.Parse(json));

            SubmitBlobRequest request = new SubmitBlobRequest();
            request.TransactionBlob = signedTx.TxBlob;

            Submit result = await client.SubmitTransactionBlob(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("tesSUCCESS", result.EngineResult);
            Assert.IsNotNull(result.Transaction.Hash);
        }

        [TestMethod]
        public async Task CanGetBookOffers()
        {
            IRippleClient rippleClient = new RippleClient("wss://s1.ripple.com:443");
            rippleClient.Connect();
            BookOffersRequest request = new BookOffersRequest();

            request.TakerGets = new Currency { CurrencyCode = "EUR", Issuer = "rhub8VRN55s94qWKDv6jmDy1pUykJzF3wq" };
            request.TakerPays = new Currency();

            //request.TakerGets = new Currency();
            //request.TakerPays = new Currency { CurrencyCode = "EUR", Issuer = "rhub8VRN55s94qWKDv6jmDy1pUykJzF3wq" };

            request.Limit = 10;

            var offers = await rippleClient.BookOffers(request);

            foreach (var bookOffer in offers.Offers)
            {
                Debug.WriteLine(bookOffer.Account);
            }

            Assert.IsNotNull(offers);
        }

        [TestMethod]
        public async Task CanSetOffer()
        {

            AccountInfo accountInfo = await client.AccountInfo("rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V");

            IOfferCreateTransaction offerCreate = new OfferCreateTransaction();
            offerCreate.Sequence = accountInfo.AccountData.Sequence;
            offerCreate.TakerGets = new Currency {ValueAsXrp = 10};
            offerCreate.TakerPays = new Currency{CurrencyCode = "XYZ", Issuer = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT", Value = "10"};
            offerCreate.Expiration = DateTime.UtcNow.AddHours(1);
            offerCreate.Account = "rwEHFU98CjH59UX2VqAgeCzRFU9KVvV71V";

            var json = offerCreate.ToString();
            TxSigner signer = TxSigner.FromSecret("xxxxxxx");
            SignedTx signedTx = signer.SignJson(JObject.Parse(json));

            SubmitBlobRequest request = new SubmitBlobRequest();
            request.TransactionBlob = signedTx.TxBlob;

            Submit result = await client.SubmitTransactionBlob(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("tesSUCCESS", result.EngineResult);
            Assert.IsNotNull(result.Transaction.Hash);

        }

        [TestMethod]
        public async Task CanGetTestNetBookOffers()
        {
            BookOffersRequest request = new BookOffersRequest();
           
            request.TakerGets = new Currency();
            request.TakerPays = new Currency { CurrencyCode = "XYZ", Issuer = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT" };

            request.Limit = 10;

            var offers = await client.BookOffers(request);

            Assert.IsNotNull(offers);            
        }

        [TestMethod]
        public async Task CanFillOrder()
        {
            AccountInfo accountInfo = await client.AccountInfo("rEqtEHKbinqm18wQSQGstmqg9SFpUELasT");

            IOfferCreateTransaction offerCreate = new OfferCreateTransaction();
            offerCreate.Sequence = accountInfo.AccountData.Sequence;
            offerCreate.TakerGets = new Currency { CurrencyCode = "XYZ", Issuer = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT", Value = "10" };
            offerCreate.TakerPays = new Currency { ValueAsXrp = 10 };
            offerCreate.Expiration = DateTime.UtcNow.AddHours(1);
            offerCreate.Account = "rEqtEHKbinqm18wQSQGstmqg9SFpUELasT";

            var json = offerCreate.ToString();
            TxSigner signer = TxSigner.FromSecret("xxxxxxx");
            SignedTx signedTx = signer.SignJson(JObject.Parse(json));

            SubmitBlobRequest request = new SubmitBlobRequest();
            request.TransactionBlob = signedTx.TxBlob;

            Submit result = await client.SubmitTransactionBlob(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("tesSUCCESS", result.EngineResult);
            Assert.IsNotNull(result.Transaction.Hash);
        }

        [TestMethod]
        public async Task CanDeleteTrust()
        {
            AccountInfo accountInfo = await client.AccountInfo("rho3u4kXc5q3chQFKfn9S1ZqUCya1xT3t4");

            ITrustSetTransaction trustSet = new TrustSetTransaction();
            trustSet.Flags = TrustSetFlags.tfSetNoRipple | TrustSetFlags.tfFullyCanonicalSig;
            trustSet.Account = "rho3u4kXc5q3chQFKfn9S1ZqUCya1xT3t4";
            trustSet.LimitAmount = new Currency {ValueAsNumber = 0, Issuer = "rDLXQ8KEBn3Aw313bGzhEemx8cCPpGha3d", CurrencyCode = "PHP"};
            trustSet.QualityIn = 0;
            trustSet.QualityOut = 0;
            trustSet.Sequence = accountInfo.AccountData.Sequence;
            trustSet.Fee = new Currency {Value = "12"};

            var json = trustSet.ToString();
            TxSigner signer = TxSigner.FromSecret("xxxxxxx");
            SignedTx signedTx = signer.SignJson(JObject.Parse(json));

            SubmitBlobRequest request = new SubmitBlobRequest();
            request.TransactionBlob = signedTx.TxBlob;

            Submit result = await client.SubmitTransactionBlob(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("tesSUCCESS", result.EngineResult);
            Assert.IsNotNull(result.Transaction.Hash);

        }
       
    }
}
