using Newtonsoft.Json;

namespace DynamoDBNPSImport
{
    public class Item
    {
        public Attribute PersonTypeName { get; set; }

        [JsonProperty("EvalutionDate")]
        public Attribute EvaluationDate { get; set; }
        public Attribute RequestId { get; set; }
        public Attribute PkOrganization { get; set; }
        public Attribute Score { get; set; }
        public Attribute M1Version { get; set; }
        public Attribute DatabaseName { get; set; }
        public Attribute OrganizationName { get; set; }
        public Attribute PkPersonType { get; set; }
        public Attribute M1Identifier { get; set; }
        public Attribute DatabaseServer { get; set; }
    }

    public class Attribute
    {
        public string S { get; set; }
        public string N { get; set; }
    }

    public class RootObject
    {
        public List<Item> Items { get; set; }
    }
}


