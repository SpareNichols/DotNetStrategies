using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DotNetStrategies.Resilience.TransientFaultHandling
{
    /// <summary>
    /// A service that performs some actions that are susceptible to transient faults.
    /// </summary>
    public class FaultProneService
    {
        /// <summary>
        /// The culprit.
        /// </summary>
        private readonly HttpClient _httpClient;

        public FaultProneService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Using Polly policies to retry HTTP requests that return responses that may be transient errors.
        /// </summary>
        public async Task ExecuteWithIncrementalBackoff()
        {
            // Not all HTTP status codes indicate a recoverable error. Generally, 4xx codes should not be
            // retried without something changing on the client's side. 5xx codes are more likely to be transient.
            var statusCodesForRetry = new List<HttpStatusCode>() {
                HttpStatusCode.BadGateway, 
                HttpStatusCode.GatewayTimeout, 
                HttpStatusCode.InternalServerError, 
                HttpStatusCode.ServiceUnavailable 
            };

            // We can use Polly Policies to define the result condition that should be handled and the action to take when that result occurs.
            // In this case, we are creating a policy that watches for HTTP responses with a status code that matches one of those
            // defined above. This policy is a wait-and-retry policy. We configure the number of times we want the retry to happen as well
            // as a function to derive how long to wait from the attempt number.
            var waitAndRetryPolicy = Policy.HandleResult<HttpResponseMessage>(result => statusCodesForRetry.Contains(result.StatusCode))
                .WaitAndRetryAsync(retryCount: 5, sleepDurationProvider: retryNumber => TimeSpan.FromSeconds(retryNumber));

            // We can then perform an action using the policy.
            await waitAndRetryPolicy.ExecuteAsync(async () => await _httpClient.SendAsync(GetRequestMessage())).ConfigureAwait(false);

            // Note that the policy is defined for either synchronous or asynchronous actions, not both.
        }

        private HttpRequestMessage GetRequestMessage()
        {
            return new HttpRequestMessage(HttpMethod.Get, "https://www.google.com");
        }
    }
}
