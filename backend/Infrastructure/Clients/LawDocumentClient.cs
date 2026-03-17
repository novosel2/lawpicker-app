using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Application.Interfaces.IClients;
using Application.Interfaces.IRepositories;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Clients;

public class LawDocumentClient : ILawDocumentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LawDocumentClient> _logger;
    private readonly ILawDocumentRepository _lawDocumentRepository;
    private readonly string sparqlUrl = "https://publications.europa.eu/webapi/rdf/sparql";

    private static readonly Dictionary<string, string> LangTo3Letter = new()
    {
        { "BG", "BUL" }, { "CS", "CES" }, { "DA", "DAN" }, { "DE", "DEU" },
        { "EL", "ELL" }, { "EN", "ENG" }, { "ES", "SPA" }, { "ET", "EST" },
        { "FI", "FIN" }, { "FR", "FRA" }, { "GA", "GLE" }, { "HR", "HRV" },
        { "HU", "HUN" }, { "IT", "ITA" }, { "LT", "LIT" }, { "LV", "LAV" },
        { "MT", "MLT" }, { "NL", "NLD" }, { "PL", "POL" }, { "PT", "POR" },
        { "RO", "RON" }, { "SK", "SLK" }, { "SL", "SLV" }, { "SV", "SWE" }
    };

    public LawDocumentClient(HttpClient httpClient, ILogger<LawDocumentClient> logger,
            ILawDocumentRepository lawDocumentRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _lawDocumentRepository = lawDocumentRepository;
    }

    public async Task<List<LawDocument>> GetLawsAsync(int limit, int offset)
    {
        var sparqlQuery = @$"
            PREFIX cdm: <http://publications.europa.eu/ontology/cdm#>
            PREFIX owl: <http://www.w3.org/2002/07/owl#>
            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
            PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>

            SELECT ?celexNumber 
            (SAMPLE(?type) AS ?type) 
            (SAMPLE(?dateDocument) AS ?dateDocument)
            (SAMPLE(?title) AS ?title)
            WHERE {{
                ?work cdm:work_date_document ?dateDocument .
                    ?work owl:sameAs ?celexUri .
                    ?work cdm:resource_legal_in-force ""true""^^xsd:boolean .

                    ?expression cdm:expression_belongs_to_work ?work .
                    ?expression cdm:expression_uses_language <http://publications.europa.eu/resource/authority/language/ENG> .
                    ?expression cdm:expression_title ?titleRaw .
                    BIND(STR(?titleRaw) AS ?title)

                    BIND(STRAFTER(STR(?celexUri), ""http://publications.europa.eu/resource/celex/"") AS ?celexNumber)

                    OPTIONAL {{ 
                        ?work cdm:resource_legal_type ?legalType .
                            BIND(STR(?legalType) AS ?type)
                    }}

                OPTIONAL {{ ?work cdm:resource_legal_date_entry-into-force ?entryDate }}
                OPTIONAL {{ ?work cdm:resource_legal_date_end-of-validity ?endDate }}

                FILTER(STRSTARTS(STR(?celexUri), ""http://publications.europa.eu/resource/celex/""))

                    FILTER(?type IN (
                                ""L"",    # Directives
                                ""R"",    # Regulations  
                                ""D"",    # Decisions (with or without addressee)
                                ""E"",    # CFSP: common positions, joint actions, common strategies
                                ""F"",    # Police and judicial cooperation in criminal matters
                                ""A"",    # Agreements (both international agreements and Member State agreements)
                                ""H"",    # Recommendations
                                ""S""     # ECSC Decisions of general interest
                                ))

                    FILTER(!BOUND(?entryDate) || ?entryDate <= NOW())

                    FILTER(!BOUND(?endDate) || ?endDate > NOW())

                    FILTER(REGEX(?celexNumber, ""^3""))
            }}
        GROUP BY ?celexNumber
            OFFSET {offset}
        LIMIT {limit}
        ";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{sparqlUrl}?query={Uri.EscapeDataString(sparqlQuery)}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sparql-results+json"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        List<LawDocument> lawDocuments = [];

        using (JsonDocument doc = JsonDocument.Parse(json))
        {
            var bindingJson = doc.RootElement.GetProperty("results").GetProperty("bindings");
            var binding = bindingJson.EnumerateArray();

            foreach(var item in binding)
            {
                LawDocument lawDocument = new LawDocument()
                {
                    Celex = item.GetProperty("celexNumber").GetProperty("value").ToString(),
                    Type = Char.Parse(item.GetProperty("type").GetProperty("value").ToString()),
                    Date = DateOnly.Parse(item.GetProperty("dateDocument").GetProperty("value").ToString()),
                    Title = item.GetProperty("title").GetProperty("value").ToString()
                };

                lawDocuments.Add(lawDocument);
            }
        }

        return lawDocuments;
    }

    public async Task<Stream?> DownloadPdfAsync(string celex, string lang)
    {
        if (!LangTo3Letter.TryGetValue(lang.ToUpper(), out var lang3))
            return null;

        var sparqlQuery = $@"
            PREFIX cdm: <http://publications.europa.eu/ontology/cdm#>
            PREFIX owl: <http://www.w3.org/2002/07/owl#>
            SELECT ?mani WHERE {{
                ?work owl:sameAs <http://publications.europa.eu/resource/celex/{celex}> .
                    ?expr cdm:expression_belongs_to_work ?work .
                    ?expr cdm:expression_uses_language <http://publications.europa.eu/resource/authority/language/{lang3}> .
                    ?mani cdm:manifestation_manifests_expression ?expr .
                    ?mani cdm:manifestation_type ?format .
                    FILTER(CONTAINS(STR(?format), ""pdf""))
            }}
        LIMIT 1";

        var sparqlRequest = new HttpRequestMessage(HttpMethod.Get, $"{sparqlUrl}?query={Uri.EscapeDataString(sparqlQuery)}");
        sparqlRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sparql-results+json"));

        var sparqlResponse = await _httpClient.SendAsync(sparqlRequest);
        if (!sparqlResponse.IsSuccessStatusCode)
            return null;

        var json = await sparqlResponse.Content.ReadAsStringAsync();
        string? manifestationUri;

        using (var doc = JsonDocument.Parse(json))
        {
            var bindings = doc.RootElement.GetProperty("results").GetProperty("bindings");
            if (bindings.GetArrayLength() == 0)
                return null;

            manifestationUri = bindings[0].GetProperty("mani").GetProperty("value").GetString();
        }

        if (string.IsNullOrEmpty(manifestationUri))
            return null;

        var pdfResponse = await _httpClient.GetAsync($"{manifestationUri}/DOC_1");
        if (!pdfResponse.IsSuccessStatusCode)
            pdfResponse = await _httpClient.GetAsync($"{manifestationUri}/DOC_2");
        if (!pdfResponse.IsSuccessStatusCode)
            return null;

        return await pdfResponse.Content.ReadAsStreamAsync();
    }
}
