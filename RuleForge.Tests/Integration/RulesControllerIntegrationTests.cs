using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RuleForge.Application.Rules.Dto;

namespace RuleForge.Tests.Integration;

[Collection("Integration")]
public sealed class RulesControllerIntegrationTests : IDisposable
{
    private readonly RuleForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RulesControllerIntegrationTests(PostgreSqlFixture fixture)
    {
        _factory = new RuleForgeWebApplicationFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Get_ApiRules_Returns200WithPagedResult()
    {
        var response = await _client.GetAsync("/api/rules");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("items");
        content.Should().Contain("totalCount");
    }

    [Fact]
    public async Task GetById_ExistingRule_Returns200WithRuleDto()
    {
        var createRequest = new CreateRuleRequest
        {
            Name = "Get Test Rule",
            IsActive = true,
            Priority = 100,
            Conditions = new ConditionDto { Field = "Amount", Operator = "GreaterThan", Value = "50" }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/rules", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<RuleDto>();

        var getResponse = await _client.GetAsync($"/api/rules/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rule = await getResponse.Content.ReadFromJsonAsync<RuleDto>();
        rule.Should().NotBeNull();
        rule!.Name.Should().Be("Get Test Rule");
    }

    [Fact]
    public async Task GetById_NonExistingRule_Returns404()
    {
        var response = await _client.GetAsync($"/api/rules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ValidCreateRuleRequest_Returns201WithLocationHeader()
    {
        var request = new CreateRuleRequest
        {
            Name = "New API Rule",
            IsActive = true,
            Priority = 50,
            Conditions = new ConditionDto { Field = "Status", Operator = "Equals", Value = "Active" }
        };

        var response = await _client.PostAsJsonAsync("/api/rules", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/rules/");
        var created = await response.Content.ReadFromJsonAsync<RuleDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("New API Rule");
    }

    [Fact]
    public async Task Put_ExistingRule_Returns200()
    {
        var createRequest = new CreateRuleRequest
        {
            Name = "To Update",
            IsActive = true,
            Priority = 10,
            Conditions = new ConditionDto { Field = "F", Operator = "O", Value = "V" }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/rules", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<RuleDto>();

        var updateRequest = new UpdateRuleRequest
        {
            Name = "Updated Name",
            IsActive = false,
            Priority = 200,
            Conditions = new ConditionDto { Field = "F", Operator = "O", Value = "V" }
        };

        var putResponse = await _client.PutAsJsonAsync($"/api/rules/{created!.Id}", updateRequest);

        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putResponse.Content.ReadFromJsonAsync<RuleDto>();
        updated!.Name.Should().Be("Updated Name");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Put_NonExistingRule_Returns404()
    {
        var updateRequest = new UpdateRuleRequest
        {
            Name = "Test",
            IsActive = true,
            Priority = 0,
            Conditions = new ConditionDto { Field = "F", Operator = "O", Value = "V" }
        };

        var response = await _client.PutAsJsonAsync($"/api/rules/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingRule_Returns204()
    {
        var createRequest = new CreateRuleRequest
        {
            Name = "To Delete",
            IsActive = true,
            Priority = 0,
            Conditions = new ConditionDto { Field = "F", Operator = "O", Value = "V" }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/rules", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<RuleDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/rules/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistingRule_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/rules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
