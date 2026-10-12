using Tyngd.Shared.DTOs;
using Tyngd.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassTemplateController : ControllerBase
{
    private readonly TyngdContext _context;

    public PassTemplateController(TyngdContext context)
    {
        _context = context;
    }

    [HttpGet("exercisecount")]
    public async Task<ActionResult<List<PassTemplateExerciseCountDto>>> GetExerciseCount()
    {
        var result = await _context.PassTemplate
            .Include(p => p.SplitTemplate)
            .Select(p => new PassTemplateExerciseCountDto
            {
                PassTemplateId   = p.PassTemplateId,
                PassTemplateName = p.PassTemplateName,
                SplitTemplateId  = p.SplitTemplateID,
                SplitTemplateName = p.SplitTemplate.SplitTemplateName,
                ExerciseCount    = _context.ExerciseTemplate.Count(e => e.PassTemplateId == p.PassTemplateId),
            })
            .ToListAsync();

        return Ok(result);
    }
}

