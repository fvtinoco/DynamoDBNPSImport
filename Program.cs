using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
using DynamoDBNPSImport;
using Newtonsoft.Json;
using System.Diagnostics;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Configuration;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var table = "NPSEvaluation";

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json")
            .Build();

        //The account profile from where to import data!
        string profileImport = configuration["Settings:ImportProfile"];

        //The JSON file to import
        var jsonFile = configuration["Settings:JSONFileName"];

        var processInfo = new ProcessStartInfo
        {
            FileName = "aws",
            Arguments = $"sso login --profile {profileImport}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process { StartInfo = processInfo })
        {
            //Force login on specified profile
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Successfully authenticated with AWS SSO.");

                var ssoCreds = LoadSSOCredentials(profileImport);

                // Display the caller's identity.
                // Not really used for anything else...just to confirm that the account number is correct! 
                var ssoProfileClient = new AmazonSecurityTokenServiceClient(ssoCreds);
                Console.WriteLine($"\nSSO Profile:\n {await ssoProfileClient.GetCallerIdentityArn()}");

                var client = new AmazonDynamoDBClient(ssoCreds, Amazon.RegionEndpoint.EUWest1);

                string jsonData = File.ReadAllText(jsonFile);
                // Deserialize the JSON into a list of DynamoDBItem
                var rootObject = JsonConvert.DeserializeObject<RootObject>(jsonData);
                List<Item> items = rootObject.Items;

                foreach (var item in items)
                {
                    var request = new PutItemRequest
                    {
                        TableName = table,
                        Item = new Dictionary<string, AttributeValue>
                {
                    { "PersonTypeName", new AttributeValue { S = item.PersonTypeName.S } },
                    { "EvaluationDate", new AttributeValue { S = item.EvaluationDate.S } },
                    { "RequestId", new AttributeValue { S = item.RequestId.S } },
                    { "PkOrganization", new AttributeValue { S = item.PkOrganization.S } },
                    { "Score", new AttributeValue { N = item.Score.N } },
                    { "M1Version", new AttributeValue { S = item.M1Version.S } },
                    { "DatabaseName", new AttributeValue { S = item.DatabaseName.S } },
                    { "OrganizationName", new AttributeValue { S = item.OrganizationName.S } },
                    { "PkPersonType", new AttributeValue { S = item.PkPersonType.S } },
                    { "M1Identifier", new AttributeValue { S = item.M1Identifier.S } },
                    { "DatabaseServer", new AttributeValue { S = item.DatabaseServer.S } }
                }
                    };

                    client.PutItemAsync(request);
                }
            }
            else
            {
                Console.WriteLine("Failed to authenticate with AWS SSO.");
            }
        }


        static AWSCredentials LoadSSOCredentials(string profile)
        {
            var chain = new CredentialProfileStoreChain();
            if (!chain.TryGetAWSCredentials(profile, out var credentials))
                throw new Exception($"Failed to find the {profile} profile");
            return credentials;
        }
    }
}

public static class Extensions
{
    public static async Task<string> GetCallerIdentityArn(this IAmazonSecurityTokenService stsClient)
    {
        var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
        return response.Arn;
    }
}