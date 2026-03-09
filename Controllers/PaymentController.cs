using Microsoft.AspNetCore.Mvc;

public class PaymentController : Controller
{
    private readonly IConfiguration _configuration;

    public PaymentController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Pay()
    {
        var model = new PaymentViewModel
        {
            PublicKey = _configuration["Squad:PublicKey"]
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Success(string refId)
    {
        ViewBag.Reference = refId;
        return View();
    }
}
