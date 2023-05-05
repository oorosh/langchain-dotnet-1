using System.Text;
using System.Text.Json;
using LangChain.NET.LLMS.OpenAi;
using LangChain.NET.Schema;
using Microsoft.DeepDev;

namespace LangChain.NET.LLMS.HuggingFace;

public class HuggingFace : BaseLLM
{
    private readonly HuggingFaceConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public HuggingFace() : this(new HuggingFaceConfiguration()){}
    
    public HuggingFace(HuggingFaceConfiguration configuration) : base(configuration)
    {
        _configuration = configuration;

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri($"https://api-inference.huggingface.co/models/{configuration.ModelName}");
            
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration.ApiKey}");
    }

    public override string ModelType { get; set; }
    public override string LLMType { get; set; }
    public override TikTokenizer Tokenizer { get; set; }
    
    public override async Task<LLMResult> Generate(string[] prompts, string[]? stop)
    {
        var huggingFaceInferenceResponses = new List<HuggingFaceInferenceResponse>();
        
        foreach (var prompt in prompts)
        {
            var result = await this._httpClient.PostAsync($"https://api-inference.huggingface.co/models/{_configuration.ModelName}", new StringContent(JsonSerializer.Serialize(new
            {
                inputs = prompt,
                parameters = new
                {
                    temperature = _configuration.Temperature
                }
            }), Encoding.UTF8, "application/json"));

            if (!result.IsSuccessStatusCode)
            {
                Console.WriteLine(await result.Content.ReadAsStringAsync());
                throw new Exception("HTTP Exception");
            }
            
            var response = JsonSerializer.Deserialize<HuggingFaceInferenceResponse[]>(await result.Content.ReadAsStreamAsync());

            huggingFaceInferenceResponses.AddRange(response);
        }
        
        return new LLMResult
        {
            Generations = huggingFaceInferenceResponses.Select(choice => new Generation()
            {
                Text = choice.GeneratedText, GenerationInfo = new Dictionary<string, object>(0)
            }).ToArray(),
            LLMOutput = new Dictionary<string, object>(0)
        };
    }
}