using System.Net;
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
    private readonly string eurLexUrl = "https://eur-lex.europa.eu/legal-content";

    public LawDocumentClient(HttpClient httpClient, ILogger<LawDocumentClient> logger,
            ILawDocumentRepository lawDocumentRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _lawDocumentRepository = lawDocumentRepository;
    }

    public async Task<List<LawDocument>> GetLawsAsync(int limit, int offset)
    {
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/sparql-results+json");
        
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
    
    # Get title from expression (English version) and clean it
    ?expression cdm:expression_belongs_to_work ?work .
    ?expression cdm:expression_uses_language <http://publications.europa.eu/resource/authority/language/ENG> .
    ?expression cdm:expression_title ?titleRaw .
    BIND(STR(?titleRaw) AS ?title)
    
    # Extract CELEX number from URI
    BIND(STRAFTER(STR(?celexUri), ""http://publications.europa.eu/resource/celex/"") AS ?celexNumber)
    
    OPTIONAL {{ 
        ?work cdm:resource_legal_type ?legalType .
        BIND(STR(?legalType) AS ?type)
    }}
    
    # Additional checks for currently active laws
    OPTIONAL {{ ?work cdm:resource_legal_date_entry-into-force ?entryDate }}
    OPTIONAL {{ ?work cdm:resource_legal_date_end-of-validity ?endDate }}
    
    # Filter to only include documents with CELEX identifiers
    FILTER(STRSTARTS(STR(?celexUri), ""http://publications.europa.eu/resource/celex/""))

    # Include all legal acts that could contain employment, cross-border compliance, and contract law
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

    # Ensure law has entered into force (if date exists)
    FILTER(!BOUND(?entryDate) || ?entryDate <= NOW())
    
    # Ensure law hasn't expired (if end date exists, it should be in future)
    FILTER(!BOUND(?endDate) || ?endDate > NOW())
    
    # Focus on EU laws (CELEX sector 3 = EU law)
    FILTER(REGEX(?celexNumber, ""^3""))
}}
GROUP BY ?celexNumber
OFFSET {offset}
LIMIT {limit}
";       
        var response = await _httpClient.GetAsync($"{sparqlUrl}?query={Uri.EscapeDataString(sparqlQuery)}");
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
        var response = await _httpClient.GetAsync($"{eurLexUrl}/{lang}/TXT/PDF/?uri=CELEX:{celex}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync();
    }
}
