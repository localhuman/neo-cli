using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.IO.Data.LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Neo.VM;
using Newtonsoft.Json.Linq;
using Neo.SmartContract;
using System.Numerics;
using WalletHelper = Neo.Wallets.Helper;
using JObject = Neo.IO.Json.JObject;
using Iterator = Neo.IO.Data.LevelDB.Iterator;
using VMArray = Neo.VM.Types.Array;
using Akka.Actor;

namespace Neo.Notifications
{

    public static class NotificationsPrefix
    {
        public const byte NP_ADDR = 0x01;
        public const byte NP_CONTRACT = 0x02;

        public const byte NP_BLOCK = 0x03;
        public const byte NP_COUNT = 0x04;

        public const byte NP_CONTRACT_LIST = 0x05;
        public const byte NP_TOKEN = 0x06;

        public const byte NP_TX = 0x07;
    }

    public class NotificationDB : UntypedActor
    {

        private DB db;
        private ReadOptions readOptions = new ReadOptions { FillCache = false };
        private WriteOptions writeOptions = WriteOptions.Default;

        private int blockEventIndex = 0;

        private static NotificationDB _instance;

        public static Block CurrentBlock;

        public static String version;

        public static Props Props(IActorRef blockchain)
        {
            return Akka.Actor.Props.Create(() => new NotificationDB(blockchain));
        }

        public static NotificationDB Instance
        {
            get
            {
                return _instance;
            }
        }

        public NotificationDB(IActorRef blockchain)
        {
            if(_instance == null)
            {
                _instance = this;
                String path = Settings.Default.Paths.Notifications;
                db = DB.Open(path, new Options { CreateIfMissing = true });
                blockchain.Tell(new Blockchain.Register());
            }
        }
        

        public NotificationResult NotificationsForBlock(uint height, NotificationQuery query)
        {
            NotificationResult nResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Results for a block", results = new List<JToken>() };
            foreach(IterResult res in IterFind(SliceBuilder.Begin(NotificationsPrefix.NP_BLOCK).Add(height))) {
                if (filter_result(res.json, query))
                {
                    nResult.results.Add(res.json);
                }
            }

            return nResult;
        }

        public NotificationResult NotificationsForContract(UInt160 contract, NotificationQuery query)
        {
            NotificationResult nResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Results for contract", results = new List<JToken>() };

            foreach (IterResult res in IterFind(SliceBuilder.Begin(NotificationsPrefix.NP_CONTRACT).Add(contract)))
            {
                if (filter_result(res.json, query))
                {
                    nResult.results.Add(res.json);
                }
            }

            return nResult;
        }

        public NotificationResult NotificationsForAddress(UInt160 address, NotificationQuery query)
        {
            NotificationResult nResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Results for contract", results = new List<JToken>() };

            foreach (IterResult res in IterFind(SliceBuilder.Begin(NotificationsPrefix.NP_ADDR).Add(address)))
            {
                if (filter_result(res.json, query))
                {
                    nResult.results.Add(res.json);
                }
            }

            return nResult;
        }

        public NotificationResult NotificationsForTransaction(UInt256 tx, NotificationQuery query)
        {
            NotificationResult nResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Results for TX", results = new List<JToken>() };

            foreach (IterResult res in IterFind(SliceBuilder.Begin(NotificationsPrefix.NP_TX).Add(tx)))
            {
                if (filter_result(res.json, query))
                {
                    nResult.results.Add(res.json);
                }
            }

            return nResult;
        }


        public NotificationResult GetTokens(NotificationQuery query)
        {
            NotificationResult nResult = new NotificationResult { current_height = Blockchain.Singleton.Height + 1, message = "Results for tokens", results = new List<JToken>() };

            foreach (IterResult res in IterFind(SliceBuilder.Begin(NotificationsPrefix.NP_TOKEN)))
            {
                if( filter_result(res.json, query))
                {
                    nResult.results.Add(res.json);
                }
            }
            return nResult;
        }


        public void Dispose()
        {

            db.Dispose();
        }

        private bool filter_result(JToken token, NotificationQuery query)
        {

            if (query.EventType == null && query.AfterBlock == -1)
            {
                return true;
            }

            if( query.EventType != null)
            {
                JToken notifyType = token.SelectToken("notify_type");
                if(notifyType != null && !query.EventType.Contains(notifyType.ToString()))
                {
                    return false;
                }
            }

            int notifyBlock = token.SelectToken("block").ToObject<int>();

            if ( query.AfterBlock > -1)
            {
                if(notifyBlock <= query.AfterBlock)
                {
                    return false;
                }
            }

            if(query.BeforeBlock > -1)
            {
                if(notifyBlock >= query.BeforeBlock)
                {
                    return false;
                }
            }

            return true;

        }

        private List<IterResult> IterFind(Slice prefix)
        {
            List<IterResult> results = new List<IterResult>();
            Iterator it = db.NewIterator(readOptions);
            for (it.Seek(prefix); it.Valid(); it.Next())
            {
                Slice key = it.Key();
                byte[] x = key.ToArray();
                byte[] y = prefix.ToArray();
                if (x.Length < y.Length) break;
                if (!x.Take(y.Length).SequenceEqual(y)) break;
                results.Add( new IterResult { key = x, value= it.Value().ToArray() });
            }
            it.Dispose();
            return results;
        }

        protected override void OnReceive(object message)
        {
            if( message is Blockchain.PersistCompleted m)
            {
                blockEventIndex = 0;
                NotificationDB.CurrentBlock = m.Block;
//                Console.WriteLine($"Current Height {NotificationDB.CurrentBlock.Index + 1}");
            }
            else if( message is Blockchain.ApplicationExecuted e)
            {

                // blockchain height isn't updated until after this event is dispatched
                uint blockHeight = NotificationDB.CurrentBlock.Index + 1;
                string txid = e.Transaction.Hash.ToString();
                byte[] tx_hash = e.Transaction.Hash.ToArray();
                bool checkedContract = false;

                foreach (ApplicationExecutionResult res in e.ExecutionResults)
                {
                    if (res.VMState.HasFlag(VMState.FAULT))
                    {
                        continue;
                    }

                    foreach (var p in res.Notifications)
                    {

                        if (!checkedContract)
                        {
                            checkedContract = true;
                            bool contractIsSaved = checkContractExists(p.ScriptHash);
                            if (!contractIsSaved)
                            {
                                checkIsNEP5(p.ScriptHash, txid);
                            }
                        }

                        try
                        {
                            JObject notificationJson = NotificationToJson(p, blockHeight, txid);

                            VMArray states = p.State as VMArray;
                            if (states != null && states.Count > 0)
                            {
                                if( states[0] is VMArray)
                                {
                                    persistNotification("unkown", p, notificationJson, blockHeight, tx_hash);
                                }
                                else
                                {
                                    string notifType = states[0].GetString();
                                    switch (notifType)
                                    {
                                        case "transfer":
                                            persistTransfer(p, notificationJson, states, blockHeight, tx_hash);
                                            break;

                                        case "refund":
                                            persistRefund(p, notificationJson, states, blockHeight, tx_hash);
                                            break;

                                        default:
                                            persistNotification(notifType, p, notificationJson, blockHeight, tx_hash);
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            Console.WriteLine($"Could not get notification state: {error.ToString()}");
                        }

                        blockEventIndex++;
                    }

                }

            }
        }

        

        private JObject NotificationToJson(NotifyEventArgs n, uint height, string txid)
        {
            JObject notification = new JObject();
            notification["contract"] = n.ScriptHash.ToString();
            notification["block"] = height;
            notification["tx"] = txid;
            notification["index"] = blockEventIndex;

            return notification;
        }

        private byte[] JObjectToBytes(JObject json)
        {
            return Encoding.Default.GetBytes(json.ToString());
        }


        private uint incrementCount(Slice toIncrement)
        {
            uint currentCount = 0;

            if (db.TryGet(readOptions, toIncrement, out Slice value))
            {
                currentCount = BitConverter.ToUInt32(value.ToArray(), 0);
            }
            currentCount += 1;

            db.Put(writeOptions, toIncrement, currentCount);

            return currentCount;
        }

        private void checkIsNEP5(UInt160 scriptHash, string txid)
        {
            try
            {
                NEP5Token token = NEP5Token.QueryIsToken(scriptHash, txid);

                if( token != null)
                {
                    Slice tokenStore = SliceBuilder.Begin(NotificationsPrefix.NP_TOKEN).Add(scriptHash.ToArray());
                    if( !db.TryGet(readOptions, tokenStore, out Slice value))
                    {
                        db.Put(writeOptions, tokenStore, JObjectToBytes(token.ToJson()));
                    }
                }

            } catch( Exception )
            {
                // this happens when you ask a non-NEP5 contract about NEP5 details
            }
        }

        private bool checkContractExists(UInt160 scriptHash)
        {
            Slice contract_exists_query = SliceBuilder.Begin(NotificationsPrefix.NP_CONTRACT_LIST).Add(scriptHash.ToArray());
            if( db.TryGet(readOptions, contract_exists_query, out Slice value))
            {

                return true;
            }

            db.Put(writeOptions, contract_exists_query, new byte[1]);

            return false;
        }


        private void persistToAddr(byte[] addr, byte[] notification)
        {
            Slice countKey = SliceBuilder.Begin(NotificationsPrefix.NP_COUNT).Add(addr);

            uint currentCount = incrementCount(countKey);

            Slice notifKey = SliceBuilder.Begin(NotificationsPrefix.NP_ADDR).Add(addr).Add(currentCount);

            db.Put(writeOptions, notifKey, notification);
        }

        private void persistToBlock(uint blockHeight, byte[] notification)
        {
            Slice countKey = SliceBuilder.Begin(NotificationsPrefix.NP_COUNT).Add(blockHeight);

            uint currentCount = incrementCount(countKey);

            Slice notifKey = SliceBuilder.Begin(NotificationsPrefix.NP_BLOCK).Add(blockHeight).Add(currentCount);

            db.Put(writeOptions, notifKey, notification);
        }

        private void persistToContract(byte[] contract, byte[] notification)
        {
            Slice countKey = SliceBuilder.Begin(NotificationsPrefix.NP_COUNT).Add(contract);

            uint currentCount = incrementCount(countKey);

            Slice notifKey = SliceBuilder.Begin(NotificationsPrefix.NP_CONTRACT).Add(contract).Add(currentCount);

            db.Put(writeOptions, notifKey, notification);
        }

        private void persistToTransaction(byte[] tx_hash, byte[] notification)
        {
            Slice countKey = SliceBuilder.Begin(NotificationsPrefix.NP_COUNT).Add(tx_hash);

            uint currentCount = incrementCount(countKey);

            Slice notifKey = SliceBuilder.Begin(NotificationsPrefix.NP_TX).Add(tx_hash).Add(currentCount);

            db.Put(writeOptions, notifKey, notification);
        }


        private void persistTransfer(NotifyEventArgs n, JObject nJson, VMArray states, uint height, byte[] tx_hash)
        {
            nJson["notify_type"] = "transfer";

            if( states.Count >= 4)
            {

                try
                {

                    string from_addr = "";
                    string to_addr = "";

                    byte[] from_ba = states[1].GetByteArray();
                    byte[] to_ba = states[2].GetByteArray();

                    bool hasFromAddress = from_ba != null && from_ba.Length == 20 ? true : false;

                    if( hasFromAddress)
                    {
                        from_addr = WalletHelper.ToAddress( new UInt160(from_ba));

                    }

                    to_addr = WalletHelper.ToAddress(new UInt160(to_ba));
                    BigInteger amt = states[3].GetBigInteger();

                    nJson["addr_from"] = from_addr;
                    nJson["addr_to"] = to_addr;
                    nJson["amount"] = amt.ToString();

                    byte[] output = JObjectToBytes(nJson);

                    persistToAddr(to_ba, output);

                    if( hasFromAddress)
                    {
                        persistToAddr(from_ba, output);
                    }

                    persistToBlock(height, output);
                    persistToContract(n.ScriptHash.ToArray(), output);
                    persistToTransaction(tx_hash, output);
                }
                catch (Exception error)
                {
                    //                    Console.WriteLine($"Could not write transfer: Hegiht: {height} States: {states}, {error.ToString()}");
                    // if we cant persist as standard transfer, we'll persist as untyped notification
                    persistNotification("transfer", n, nJson, height, tx_hash);
                }

            }
        }

        private void persistRefund(NotifyEventArgs n, JObject nJson, VMArray states, uint height, byte[] tx_hash)
        {
            nJson["notify_type"] = "refund";

            if (states.Count >= 3)
            {
                try
                {
                    string to_addr = "";
                    byte[] to_ba = states[1].GetByteArray();
                    to_addr = WalletHelper.ToAddress(new UInt160(to_ba));
                    BigInteger amt = states[2].GetBigInteger();

                    nJson["addr_to"] = to_addr;
                    nJson["amount"] = amt.ToString();
                    nJson["state"] = null;
                    if( states.Count >= 4)
                    {
                        nJson["asset"] = states[3].GetString();
                    }

                    byte[] output = JObjectToBytes(nJson);

                    persistToAddr(to_ba, output);
                    persistToBlock(height, output);
                    persistToContract(n.ScriptHash.ToArray(), output);
                    persistToTransaction(tx_hash, output);

                }
                catch (Exception error)
                {
                    //                    Console.WriteLine($"Could not write transfer: {error.ToString()}");
                    // if we cant persist as standard refund, we'll persist as untyped notification
                    persistNotification("refund", n, nJson, height, tx_hash);

                }

            }
        }

        private void persistNotification(string notifType, NotifyEventArgs n, JObject nJson, uint height, byte[] tx_hash)
        {
            UInt160 contractHash = n.ScriptHash;
            try
            {
                nJson["notify_type"] = notifType;
                nJson["state"] = n.State.ToParameter().ToJson();
                byte[] output = JObjectToBytes(nJson);

                persistToBlock(height, output);
                persistToContract(n.ScriptHash.ToArray(), output);
                persistToTransaction(tx_hash, output);
            } catch(Exception e)
            {
                Console.WriteLine($"Could not persist {notifType} notification: {e.Message}");
            }
        }
    }
}
