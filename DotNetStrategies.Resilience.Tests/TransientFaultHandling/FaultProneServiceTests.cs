using DotNetStrategies.Resilience.TransientFaultHandling;
using Moq;
using Moq.Protected;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DotNetStrategies.Resilience.Tests.TransientFaultHandling
{
    public class FaultProneServiceTests
    {
        [Fact]
        public async Task ExecuteWithIncrementalBackoff_WhenFirstResponseIsBad_ShouldExecuteAgain()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // Setup HTTP Client message handler mock to sequence results
            handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                // First request fails with 503 Service Unavailable
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable))
                // Second request succeeds with 200 OK
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            var httpClient = new HttpClient(handlerMock.Object);

            var sut = new FaultProneService(httpClient);

            // Act
            await sut.ExecuteWithIncrementalBackoff();

            // Assert
            // We expect for the request to be sent 2 times since the first time will fail
            handlerMock.Protected().Verify("SendAsync", Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }
}
