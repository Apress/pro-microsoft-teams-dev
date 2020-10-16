
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
//https://github.com/OfficeDev/microsoft-teams-sample-outgoing-webhook/blob/master/WebhookSampleBot/Models/AuthProvider.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CompanyApp.Models
{
    public class AuthProvider
    {
        /// <summary>
        /// A dictionary for storing signing keys. Here, the look up key is based on the value of the query parameter 'id'.
        /// The signing keys must be valid 256 bit base64 encoded strings that are provided during custom bot registration in MS Teams client.
        /// </summary>
        private static readonly Dictionary<string, string> SigningKeyDictionary = new Dictionary<string, string>()
            {
                {"pro teams development", "uFJo3d4MVbZCM/8O4InlQls3VxzABjSiRn1ABnAlvTE=" },
                {"fabrikam", "QgyNSToQjf4p6+YzDpjKks1/tXeJQ7FhVHqRwTnugVI=" }
            };

        /// <summary>
        /// Validates the specified authentication header value.
        /// </summary>
        /// <param name="authenticationHeaderValue">The authentication header value present on request.</param>
        /// <param name="messageContent">Content of the HTTP message read as string.</param>
        /// <param name="claimedSenderId">The claimed sender identifier.</param>
        /// <returns>Response containing result of validation.</returns>
        public static AuthResponse Validate(string authenticationHeaderValue, string messageContent, string claimedSenderId)
        {
            if (string.IsNullOrEmpty(claimedSenderId))
            {
                return new AuthResponse(false, "Id not present on request.");
            }

            if (authenticationHeaderValue == null)
            {
                return new AuthResponse(false, "Authentication header not present on request.");
            }

            if (!authenticationHeaderValue.StartsWith("HMAC"))
            {
                return new AuthResponse(false, "Incorrect authorization header scheme.");
            }

            claimedSenderId = claimedSenderId.ToLower();
            string signingKey = null;
            if (!AuthProvider.SigningKeyDictionary.TryGetValue(claimedSenderId, out signingKey))
            {
                return new AuthResponse(false, string.Format("Signing key for {0} is not configured", claimedSenderId));
            }

            // Reject all empty messages
            if (string.IsNullOrEmpty(messageContent))
            {
                return new AuthResponse(false, "Unable to validate authentication header for messages with empty body.");
            }

            string providedHmacValue = authenticationHeaderValue.Replace("HMAC ", "");
            string calculatedHmacValue = null;
            try
            {
                byte[] serializedPayloadBytes = Encoding.UTF8.GetBytes(messageContent);

                byte[] keyBytes = Convert.FromBase64String(signingKey);
                using (HMACSHA256 hmacSHA256 = new HMACSHA256(keyBytes))
                {
                    byte[] hashBytes = hmacSHA256.ComputeHash(serializedPayloadBytes);
                    calculatedHmacValue = Convert.ToBase64String(hashBytes);
                }

                if (string.Equals(providedHmacValue, calculatedHmacValue))
                {
                    return new AuthResponse(true, null);
                }
                else
                {
                    string errorMessage = string.Format(
                        "AuthHeaderValueMismatch. Expected:'{0}' Provided:'{1}'",
                        calculatedHmacValue,
                        providedHmacValue);
                    return new AuthResponse(false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occcured while verifying HMAC on the incoming request. Exception: {0}", ex);
                return new AuthResponse(false, "Exception thrown while verifying MAC on incoming request.");
            }
        }
    }
}

