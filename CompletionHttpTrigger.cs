using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace openAIBot
{
    public class CompletionHttpTrigger
    {
        private readonly ILogger _logger;

        public CompletionHttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CompletionHttpTrigger>();
        }

        [Function("CompletionHttpTrigger")]
        [OpenApiOperation(operationId: nameof(CompletionHttpTrigger.Run), tags: new[] { "ChatGPT" })]
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Required = true, Description = "The request body")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The BadRequest response")]
        
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "completions")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var prompt = req.ReadAsString();

            // openAI 서비스 호출을 위한 HTTP 클라이언트 생성 및 인증 헤더 추가
            using var httpClient = new HttpClient();
            var apiKey = Environment.GetEnvironmentVariable("AI_ApiKey");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            // openAI 서비스 호출을 위한 요청 바디 생성
            var requestBody = JsonSerializer.Serialize(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant. You are very good at summarizing the given text into 2-3 bullet points." },
                    new { role = "user", content = prompt }
                },
                model = "gpt-3.5-turbo",
                max_tokens = 800,
                temperature = 0.7f,
            });

            // openAI 서비스 호출
            var endpoint = Environment.GetEnvironmentVariable("AI_Endpoint");
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, content);
            
            // 응답 바디에서 content 필드만 추출
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic responseJson = JsonSerializer.Deserialize<dynamic>(responseBody);
            string message = responseJson.choices[0].message.content;

            // openAI 서비스 호출 결과를 HTTP 응답으로 반환
            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            httpResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            httpResponse.WriteString(message);

            return httpResponse;
        }
    }
}
