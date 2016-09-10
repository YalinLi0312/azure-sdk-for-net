﻿namespace Microsoft.Rest.ClientRuntime.Azure.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// This class represents the connection string being set by the user
    /// e.g. TEST_CSM_ORGID_AUTHENTICATION="AADTenant=72f98AAD-86f1-2d7cd011db47;ServicePrincipal=72f98AAD-86f1-2d7cd011db47;Password=tzT2+LJBRkSAursui7/Qgo+hyQQ=;SubscriptionId=5562fbd2-HHHH-WWWW-a55d-lkjsldkjf;BaseUri=https://management.azure.com/;AADAuthEndpoint=https://login.windows.net/;GraphUri=https://graph.windows.net/"
    /// </summary>
    public class ConnectionString
    {
        #region fields
        Dictionary<string, string> _keyValuePairs;
        string _connString;
        StringBuilder _parseErrorSb;
        #endregion

        #region Properties

        private bool CheckViolation { get; set; }

        

        /// <summary>
        /// Represents key values pairs for the parsed connection string
        /// </summary>
        public Dictionary<string,string> KeyValuePairs
        {
            get
            {
                if(_keyValuePairs == null)
                {
                    _keyValuePairs = new Dictionary<string, string>();
                }
                return _keyValuePairs;
            }
            private set
            {
                _keyValuePairs = value;
            }
        }
        
        /// <summary>
        /// Returns all the parse errors while parsing connection string
        /// </summary>
        public string ParseErrors
        {
            get
            {
                if(_parseErrorSb == null)
                {
                    _parseErrorSb = new StringBuilder();                    
                }
                return _parseErrorSb.ToString();
            }

            private set
            {
                if (_parseErrorSb == null)
                {
                    _parseErrorSb = new StringBuilder();
                    _parseErrorSb.AppendLine(value);
                }
                else
                    _parseErrorSb.AppendLine(value);
            }
        }

        #endregion

        #region Constructor/Init
        void Init()
        {
            List<string> connectionKeyNames = (from fi in typeof(ConnectionStringKeys).GetFields(BindingFlags.Public | BindingFlags.Static) select fi.GetRawConstantValue().ToString()).ToList<string>();
            connectionKeyNames.ForEach((li) => KeyValuePairs.Add(li.ToLower(), string.Empty));
        }
        
        /// <summary>
        /// 
        /// </summary>
        public ConnectionString()
        {
            Init();
        }

        /// <summary>
        /// Initialize Connection string object using provided connectionString
        /// </summary>
        /// <param name="connString">Semicolon separated KeyValue pair connection string</param>
        public ConnectionString(string connString) : this(connString, true)
        { }

        public ConnectionString(string connString, bool checkViolation):this()
        {
            _connString = connString;
            CheckViolation = checkViolation;
            Parse(_connString);
            NormalizeKeyValuePairs();

            if (CheckViolation)
            {
                DetectViolations();
            }
        }
        #endregion

        #region private
        /// <summary>
        /// Update values to either default values or normalize values across key/value pairs
        /// For e.g. If ServicePrincipal is provided and password is provided, we assume password is ServicePrincipalSecret
        /// </summary>
        private void NormalizeKeyValuePairs()
        {
            string clientId, spn, password, spnSecret, userId;
            KeyValuePairs.TryGetValue(ConnectionStringKeys.AADClientIdKey.ToLower(), out clientId);
            KeyValuePairs.TryGetValue(ConnectionStringKeys.ServicePrincipalKey.ToLower(), out spn);

            KeyValuePairs.TryGetValue(ConnectionStringKeys.UserIdKey.ToLower(), out userId);
            KeyValuePairs.TryGetValue(ConnectionStringKeys.PasswordKey.ToLower(), out password);
            KeyValuePairs.TryGetValue(ConnectionStringKeys.ServicePrincipalSecretKey.ToLower(), out spnSecret);            

            //ClientId was provided and servicePrincipal was empty, we want ServicePrincipal to be initialized
            //At some point we will deprecate ClientId keyName
            if (!string.IsNullOrEmpty(clientId) && (string.IsNullOrEmpty(spn)))
            {
                KeyValuePairs[ConnectionStringKeys.ServicePrincipalKey.ToLower()] = clientId;
            }

            //Set the value of PasswordKey to ServicePrincipalSecret ONLY if userId is empty
            //If UserId is not empty, we are not sure if it's a password for inter active login or ServicePrincipal SecretKey
            if (!string.IsNullOrEmpty(password) && (string.IsNullOrEmpty(spnSecret)) && (string.IsNullOrEmpty(userId)))
            {
                KeyValuePairs[ConnectionStringKeys.ServicePrincipalSecretKey.ToLower()] = password;
            }

            //Initialize default values if found empty
            if (string.IsNullOrEmpty(clientId) && (string.IsNullOrEmpty(spn)))
            {
                KeyValuePairs.UpdateDictionary(ConnectionStringKeys.ServicePrincipalKey, "1950a258-227b-4e31-a9cf-717495945fc2");
            }

            //Initialize raw tokens to a non-null/non-empty strings
            KeyValuePairs.UpdateDictionary(ConnectionStringKeys.RawTokenKey, ConnectionStringKeys.RawTokenKey);
            KeyValuePairs.UpdateDictionary(ConnectionStringKeys.RawGraphTokenKey, ConnectionStringKeys.RawGraphTokenKey);
        }

        /// <summary>
        /// Detect any connection string violations
        /// </summary>
        private void DetectViolations()
        {
            //So far only 1 violation is being checked
            //We should also check if a supported Environment is provided in connection string (but seems we do not throw exception for unsupported environment)
            bool envSet = IsEnvironmentSet();
            string nonEmptyUriKey = FirstNonNullUriInConnectionString();

            if(envSet)
            {
                if(!string.IsNullOrEmpty(nonEmptyUriKey))
                {
                    string envName = KeyValuePairs.GetValueUsingCaseInsensitiveKey(ConnectionStringKeys.EnvironmentKey);
                    string uriKeyValue = KeyValuePairs.GetValueUsingCaseInsensitiveKey(nonEmptyUriKey);
                    string envAndUriConflictError = string.Format("Connection string contains Environment '{0}' and '{1}={2}'. Any Uri and environment cannot co-exist. Either set any environment or provide Uris", envName, nonEmptyUriKey, uriKeyValue);
                    throw new ArgumentException(envAndUriConflictError);
                }
            }
        }

        /// <summary>
        /// Find if any of the URI values has been set in the connection string
        /// </summary>
        /// <returns>First non empty URI value</returns>
        private string FirstNonNullUriInConnectionString()
        {
            string nonEmptyUriKeyName = string.Empty;
            var nonEmptyUriList = KeyValuePairs.Where(item =>
            {
                return ((item.Key.Contains("uri") || (item.Key.Equals(ConnectionStringKeys.AADAuthenticationEndpointKey.ToLower()))) && (!string.IsNullOrEmpty(item.Value)));
            });
            
            if(nonEmptyUriList.IsAny<KeyValuePair<string,string>>())
            {
                nonEmptyUriKeyName = nonEmptyUriList.FirstOrDefault().Key;
            }

            return nonEmptyUriKeyName;
        }

        /// <summary>
        /// Detects if Environment was set in the connection string
        /// </summary>
        /// <returns>True: If valid environment was set. False:If environment was empty or invalid</returns>
        private bool IsEnvironmentSet()
        {
            bool envSet = false;            
            string envNameString = KeyValuePairs.GetValueUsingCaseInsensitiveKey(ConnectionStringKeys.EnvironmentKey);
            if (!string.IsNullOrEmpty(envNameString))
            {
                EnvironmentNames envName;
                if (!Enum.TryParse<EnvironmentNames>(envNameString, out envName))
                {
                    string envError = string.Format("Environment '{0}' is not valid. Possible values:'{1}'", envNameString, envName.ListValues());
                    ParseErrors = envError;
                    throw new ArgumentException(envError);
                }

                envSet = !string.IsNullOrEmpty(envName.ToString());
            }

            return envSet;
        }
       
        #endregion

        #region Public Functions
        /// <summary>
        /// Parses connection string
        /// </summary>
        /// <param name="connString">Semicolon delimented KeyValue pair(e.g. KeyName1=value1;KeyName2=value2;KeyName3=value3)</param>
        public void Parse(string connString)
        {
            string keyName;
            string parseRegEx = @"(?<KeyName>[^=]+)=(?<KeyValue>.+)";

            if (_parseErrorSb != null) _parseErrorSb.Clear();

            if (string.IsNullOrEmpty(connString))
            {
                ParseErrors = "Empty connection string";
            }
            else
            {
                string[] pairs = connString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (pairs == null) ParseErrors = string.Format("'{0}' unable to parse string", connString);

                //TODO: Shall we clear keyValue dictionary?
                //What if parsed gets called on the same instance multiple times
                //the connectionString is either malformed/invalid
                //For now clearing keyValue dictionary, we assume the caller wants to parse new connection string
                //and wants to discard old values (even if they are valid)

                KeyValuePairs.Clear(true);
                foreach (string pair in pairs)
                {
                    Match m = Regex.Match(pair, parseRegEx);

                    if (m.Groups.Count > 2)
                    {
                        keyName = m.Groups["KeyName"].Value.ToLower();
                        if (KeyValuePairs.ContainsKey(keyName))
                        {
                            KeyValuePairs[keyName] = m.Groups["KeyValue"].Value;
                        }
                        else
                        {
                            ParseErrors = string.Format("'{0}' invalid keyname", keyName);
                        }
                    }
                    else
                    {
                        ParseErrors = string.Format("Incorrect '{0}' keyValue pair format", pair);
                    }
                }

                //Normalize AADClientId/ServicePrincipal AND Password/ServicePrincipalSecret
                NormalizeKeyValuePairs();
            }
        }
        
        /// <summary>
        /// Returns value for the key set in the connection string
        /// </summary>
        /// <param name="keyName">KeyName set in connection string</param>
        /// <returns>Value for the key provided</returns>
        public string GetValue(string keyName)
        {
            return KeyValuePairs.GetValueUsingCaseInsensitiveKey(keyName);
        }

        /// <summary>
        /// Returns conneciton string
        /// </summary>
        /// <returns>ConnectionString</returns>
        public override string ToString()
        {
            return _connString;
        }

        #endregion
    }

    /*
    /// <summary>
    /// Extension method class
    /// </summary>
    public static partial class ExtMethods
    {
        /// <summary>
        /// Allows you to clear only values or key/value both
        /// </summary>
        /// <param name="dictionary">Dictionary<string,string> that to be cleared</param>
        /// <param name="clearValuesOnly">True: Clears only values, False: Clear keys and values</param>
        public static void Clear(this Dictionary<string, string> dictionary, bool clearValuesOnly)
        {
            //TODO: can be implemented for generic dictionary, but currently there is no requirement, else the overload
            //will be reflected for the entire solution for any kind of Dictionary, so currently only scoping to Dictionary<string,string>
            if (clearValuesOnly)
            {
                foreach (string key in dictionary.Keys.ToList<string>())
                {
                    dictionary[key] = string.Empty;
                }
            }
            else
            {
                dictionary.Clear();
            }
        }

        public static string ListValues(this EnvironmentNames env)
        {
            List<string> enumValues = (from ev in typeof(EnvironmentNames).GetMembers(BindingFlags.Public | BindingFlags.Static) select ev.Name).ToList();
            return string.Join(",", enumValues.Select((item) => item));
        }

        public static bool IsAny<T>(this IEnumerable<T> collection)
        {
            return (collection != null && collection.Any());
        }
    }

    */
}
