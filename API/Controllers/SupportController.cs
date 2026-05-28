using Application.Core.Pagination;
using Application.SupportAssist.Commands;
using Application.SupportAssist.DTOs;
using Application.SupportAssist.Extensions;
using Application.SupportAssist.Queries;
using Domain.Enumerators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class SupportController : BaseApiController
{
    #region HttpGet Methods


    [Authorize(Policy = "RegisterOrAdmin")]
    [HttpGet]
    [ProducesResponseType(typeof(List<SupportTicketDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PagedList<SupportTicketDto>>> GetSupportTickets([FromQuery] SupportTicketParams supportTicketParams)
    {
        return await HandlePagedQuery(new GetSupportTicketsQuery { TicketParams = supportTicketParams });
    }

    [Authorize(Roles = Roles.Registered)]
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<SupportTicketDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<PagedList<SupportTicketDto>>> GetMyTickets([FromQuery] SupportTicketParams parameters)
    {
        return await HandlePagedQuery(new GetMyTicketsQuery(parameters));
    }

    [Authorize(Policy = "RegisterOrAdmin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupportTicketDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<SupportTicketDto>> GetSupportTicket(string id)
    {
        return await HandleQuery(new GetSupportTicketDetailsQuery { TicketId = id });
    }

    #endregion
   
    #region HttpPost methods

    [Authorize(Roles = Roles.Registered)]
    [HttpPost]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<string>> CreateSupportTicket(CreateSupportTicketDto supportTicket)
    {
        return await HandleCommand(new CreateSupportTicketCommand { TicketDto = supportTicket });
    }
    
    #endregion

    #region HttpPut methods
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UpdateSupportTicket(string id, [FromBody] UpdateSupportTicketDto supportTicket)
    {
        return await HandleCommand(new UpdateSupportTicketCommand { TicketId = id, TicketDto = supportTicket });
    }

    #endregion

    #region HttpDelete methods
    
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> DeleteSupportTicket(string id)
    {
        return await HandleCommand(new DeleteSupportTicketCommand { TicketId = id });
    }

    #endregion
}