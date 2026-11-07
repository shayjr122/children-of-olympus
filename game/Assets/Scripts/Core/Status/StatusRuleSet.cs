using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>The interaction rules in play (AD-10), evaluated in list order by every StatusContainer's fold.</summary>
    [CreateAssetMenu(menuName = "GMR/Status/Status Rule Set", fileName = "StatusRuleSet_")]
    public sealed class StatusRuleSet : ScriptableObject
    {
        [SerializeField] StatusInteractionRule[] _rules = System.Array.Empty<StatusInteractionRule>();

        public StatusInteractionRule[] Rules { get => _rules ?? System.Array.Empty<StatusInteractionRule>(); internal set => _rules = value; }
    }
}
