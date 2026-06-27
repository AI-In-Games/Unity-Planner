using System.Collections.Generic;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor.DomainValidation
{
    public interface IDomainValidationRule
    {
        IEnumerable<DomainIssue> Check(DomainAsset domain);
    }
}
