using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompanyApp.Models
{
    public class AuthResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthResponse"/> class.
        /// </summary>
        /// <param name="authSuccessful">if set to <c>true</c> then [authentication was successful].</param>
        /// <param name="errorMessage">The error message.</param>
        public AuthResponse(bool authSuccessful, string errorMessage)
        {
            this.AuthSuccessful = authSuccessful;
            this.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets a value indicating whether [authentication successful].
        /// </summary>
        /// <value>
        /// <c>true</c> if [authentication successful]; otherwise, <c>false</c>.
        /// </value>
        public bool AuthSuccessful { get; private set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string ErrorMessage { get; private set; }
    }
}

