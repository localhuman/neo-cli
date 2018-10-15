using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using Neo.IO.Json;
using WalletHelper = Neo.Wallets.Helper;

namespace Neo.Notifications
{
    public class NEP5Token
    {

        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
        public string ScriptHash { get; set; }
        public string Address { get; set; }
        public int BlockHeight { get; set; }
        public string tx { get; set; }

        public ContractState Contract { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["block"] = BlockHeight;
            json["tx"] = tx;

            JObject token = new JObject();
            json["token"] = token;
            token["name"] = Name;
            token["symbol"] = Symbol;
            token["decimals"] = Decimals;
            token["script_hash"] = ScriptHash;
            token["contract_address"] = Address;
            JObject contract = Contract.ToJson();
            contract["script"] = null;
            json["contract"] = contract;

            return json;
        }


        public static NEP5Token QueryIsToken(UInt160 scriptHash, string txid)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, "decimals");
                sb.EmitAppCall(scriptHash, "name");
                sb.EmitAppCall(scriptHash, "symbol");
                script = sb.ToArray();
            }
            ApplicationEngine engine = ApplicationEngine.Run(script);
            if (engine.State.HasFlag(VMState.FAULT)) throw new ArgumentException();

            
            NEP5Token result = new NEP5Token { ScriptHash = scriptHash.ToString() };
            result.Symbol = engine.ResultStack.Pop().GetString();
            result.Name = engine.ResultStack.Pop().GetString();
            result.Decimals = (int)engine.ResultStack.Pop().GetBigInteger();
            
            if( result.Symbol.Length > 0 && result.Name.Length > 0)
            {
                result.Address = WalletHelper.ToAddress(scriptHash);

                result.Contract = Blockchain.Singleton.GetSnapshot().Contracts.TryGet(scriptHash);
                result.BlockHeight = (int)Blockchain.Singleton.Height + 1;
                result.tx = txid;
                return result;
            }

            return null;
        }
    }
}
