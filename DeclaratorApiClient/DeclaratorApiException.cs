using System;
using System.Net.Http;

namespace TI.Declarator.DeclaratorApiClient
{
    internal class DeclaratorApiException : Exception
    {
        public HttpResponseMessage ResponseMessage { get; }

        public DeclaratorApiException(HttpResponseMessage response, string msg)
            : base(msg + $" status: {response.ReasonPhrase}") => this.ResponseMessage = response;
    }
}
