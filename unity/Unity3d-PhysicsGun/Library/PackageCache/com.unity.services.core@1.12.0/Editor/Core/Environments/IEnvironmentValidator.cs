using System.Threading.Tasks;

namespace Unity.Services.Core.Editor.Environments
{
    interface IEnvironmentValidator
    {
        Task<ValidationResult> ValidateEnvironmentAsync();
    }
}
