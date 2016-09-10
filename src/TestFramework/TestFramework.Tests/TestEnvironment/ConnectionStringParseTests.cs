﻿namespace TestFramework.Tests.TestEnvironment
{
    using Microsoft.Rest.ClientRuntime.Azure.TestFramework;
    using System.Collections.Generic;
    using System.Text;
    using Xunit;

    /// <summary>
    /// Tests for ConnectionString parsing
    /// </summary>
    public class ConnectionStringParseTests
    {
        ConnectionString connStr;
        public ConnectionStringParseTests()
        {
            connStr = new ConnectionString();
        }

        [Fact]
        public void AllPossibleValueString()
        {
            //Legal Connection string
            string legalStr = CreateConnStrWithAllPossibleValues();
            ConnectionString cs = new ConnectionString(legalStr, false);
            cs.Parse(legalStr);
            foreach(KeyValuePair<string, string> kv in cs.KeyValuePairs)
            {
                Assert.NotEmpty(kv.Value);
            }

            Assert.True(string.IsNullOrEmpty(cs.ParseErrors));
        }

        [Fact]
        public void EmptyString()
        {
            // empty Connection string
            string emptyStr = @"";
            connStr.Parse(emptyStr);
            Assert.False(string.IsNullOrEmpty(connStr.ParseErrors));
        }

        [Fact]
        public void NullString()
        {
            // null Connection string            
            connStr.Parse(null);
            Assert.False(string.IsNullOrEmpty(connStr.ParseErrors));
        }

        [Fact]
        public void NoKeyValueString()
        {
            // missingKeyValue Connection string
            string missingKeyValue = @";;;;;;;;;;;";
            connStr.Parse(missingKeyValue);            
            Assert.True(string.IsNullOrEmpty(connStr.ParseErrors));
        }

        [Fact]
        public void InvalidKeyValuePairString()
        {
            // invalid KeyValue Connection string
            string legalConnStrWithInvalidKeyNames = @"foo=bar;hello=world";
            connStr.Parse(legalConnStrWithInvalidKeyNames);
            Assert.False(string.IsNullOrEmpty(connStr.ParseErrors));
        }
                
        [Fact]
        public void MalformedString()
        {
            // malformed Connection string
            string malformedStr = @"ServicePrincipal=helloworld;AADTenant=;=;userid=foo@foo.com";
            connStr.Parse(malformedStr);
            Assert.False(string.IsNullOrEmpty(connStr.ParseErrors));
        }

        [Fact]
        public void KeyValueWithEqualSignString()
        {
            string keyValWithEqualSign = @"ServicePrincipal=Hello;ServicePrincipalSecret=234ghyu=;Password=He====;UserId===========";
            ConnectionString cs = new ConnectionString(keyValWithEqualSign);
            Assert.Equal(string.Empty, connStr.ParseErrors);

            //We parse and store all keys lowercase to avoid casing issues
            Assert.NotEqual(string.Empty, cs.KeyValuePairs["ServicePrincipal".ToLower()]);
            Assert.NotEqual(string.Empty, cs.KeyValuePairs["ServicePrincipalSecret".ToLower()]);
            Assert.NotEqual(string.Empty, cs.KeyValuePairs["Password".ToLower()]);
            Assert.NotEqual(string.Empty, cs.KeyValuePairs["UserId".ToLower()]);
        }

        [Fact]
        public void ClientIdButNotSPN()
        {
            string clientIdButNoSPN = @"AADClientId=alsdkfjalakdsjflasdj;AADTenant=asdlkfjalsdkjflaksdj;Password=laksdjlfsd00980980=";
            connStr.Parse(clientIdButNoSPN);
            Assert.Equal(string.Empty, connStr.ParseErrors);

            Assert.NotEqual(string.Empty, connStr.KeyValuePairs["ServicePrincipal".ToLower()]);
            Assert.NotEqual(string.Empty, connStr.KeyValuePairs["ServicePrincipalSecret".ToLower()]);
        }

        [Fact]
        public void UserIdAndPasswordButNoSPNSecret()
        {
            string clientIdButNoSPN = @"AADClientId=alsdkfjalakdsjflasdj;UserId=Hello@world.com;Password=laksdjlfsd00980980=";
            connStr.Parse(clientIdButNoSPN);
            Assert.Equal(string.Empty, connStr.ParseErrors);

            //ServicePrincipal will be updated with AADClientId
            Assert.NotEqual(string.Empty, connStr.KeyValuePairs["ServicePrincipal".ToLower()]);

            //As userId is non-empty, we cannot assume password is ServicePrincipalSecretKey and so will not be updated
            Assert.Equal(string.Empty, connStr.KeyValuePairs["ServicePrincipalSecret".ToLower()]);
        }

        private string CreateConnStrWithAllPossibleValues()
        {
            string sampleUrl = "http://www.somefoo.com";
            string sampleStrValue = "34rghytukbnju7HelloWorld!!lkjdfuhgghj";
            string sampleNumericValue = "3476834rghh9876";

            ConnectionString cnnStr = new ConnectionString("", false);
            StringBuilder sb = new StringBuilder();
            foreach(KeyValuePair<string, string> kv in cnnStr.KeyValuePairs)
            {
                if (kv.Key.ToLower().EndsWith("uri"))
                {
                    sb.AppendFormat("{0}={1};", kv.Key, sampleUrl);
                }
                else if (kv.Key.ToLower().EndsWith("id"))
                {
                    sb.AppendFormat("{0}={1};", kv.Key, sampleNumericValue);
                }
                else
                {
                    sb.AppendFormat("{0}={1};", kv.Key, sampleStrValue);
                }
            }

            return sb.ToString();
        }
    }
}
