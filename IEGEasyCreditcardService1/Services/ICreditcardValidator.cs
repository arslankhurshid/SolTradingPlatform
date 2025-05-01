using IEGEasyCreditcardService.Models;

namespace IEGEasyCreditcardService.Services
{
    public interface ICreditcardValidator
    {
        bool IsValid(CreditcardTransaction transaction);
    }
}
