using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AsyncMessaging;

public class IndexModel : PageModel
{
    private readonly MessageSenderService _senderService;

    public IndexModel(MessageSenderService senderService, IOptions<AzureServiceBusOptions> options)
    {
        _senderService = senderService;
        QueueName = options.Value.QueueName;
    }

    [BindProperty]
    public string User { get; set; }

    [BindProperty]
    public string Text { get; set; }

    public string StatusMessage { get; set; }

    public string QueueName { get; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        await _senderService.SendMessageAsync(User, Text);
        StatusMessage = $"Nachricht erfolgreich gesendet: \"{Text}\" von {User}";
        return Page();
    }
}
