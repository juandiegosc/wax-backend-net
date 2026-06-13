using Application.Quotation.Commands;
using Application.Quotation.DTOs;
using Application.Quotation.Queries;
using Domain.Enumerators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = Roles.Admin)]
public class QuotationRulesController : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(QuotationRuleDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<QuotationRuleDto>> CreateQuotationRule([FromBody] CreateQuotationRuleDto dto)
    {
        return await HandleCommand(new CreateQuotationRuleCommand { Dto = dto });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<QuotationRuleDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IReadOnlyList<QuotationRuleDto>>> GetQuotationRules([FromQuery] bool? activeOnly)
    {
        return await HandleQuery(new GetQuotationRulesQuery { ActiveOnly = activeOnly });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(QuotationRuleDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<QuotationRuleDto>> GetQuotationRuleById(string id)
    {
        return await HandleQuery(new GetQuotationRuleByIdQuery { Id = id });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(QuotationRuleDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<QuotationRuleDto>> UpdateQuotationRule(string id, [FromBody] UpdateQuotationRuleDto dto)
    {
        return await HandleCommand(new UpdateQuotationRuleCommand { Id = id, Dto = dto });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteQuotationRule(string id)
    {
        return await HandleCommand(new DeleteQuotationRuleCommand { Id = id });
    }
}
