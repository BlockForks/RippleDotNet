﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RippleDotNet.Model.Ledger
{
    public class AccountRootLedgerObject : BaseRippleLedgerObject
    {
        public AccountRootLedgerObject()
        {
            LedgerEntryType = LedgerEntryType.AccountRoot;
        }

        public string Account { get; set; }

        public AccountRootFlags Flags { get; set; }

        public uint Sequence { get; set; }

        public string Balance { get; set; }

        public uint OwnerCount { get; set; }

        [JsonProperty("PreviousTxnID")]
        public string PreviousTransactionId { get; set; }

        [JsonProperty("PreviousTxnLgrSeq")]
        public uint PreviousTransactionLedgerSequence { get; set; }

        [JsonProperty("AccountTxnID")]
        public string AccountTransactionId { get; set; }

        public string RegularKey { get; set; }

        public string EmailHash { get; set; }

        public string MessageKey { get; set; }

        public byte? TickSize { get; set; }

        public uint? TransferRate { get; set; }

        public string Domain { get; set; }
    }
}
