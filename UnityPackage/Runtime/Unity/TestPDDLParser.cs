using UnityEngine;
using AIInGames.Planning.PDDL;

namespace AIInGames.Planning.Unity
{
    /// <summary>
    /// Simple test script to verify PDDL parser is working.
    /// Attach to a GameObject and check the console for output.
    /// </summary>
    public class TestPDDLParser : MonoBehaviour
    {
        [TextArea(10, 20)]
        public string testPddl = @"(define (domain test-domain)
  (:requirements :strips :typing)
  (:types location item)
  (:predicates
    (at ?obj - item ?loc - location)
    (connected ?from ?to - location))
  (:action move
    :parameters (?from ?to - location)
    :precondition (connected ?from ?to)
    :effect (at item1 ?to))
)";

        void Start()
        {
            TestParser();
        }

        void TestParser()
        {
            Debug.Log("Testing PDDL Parser...");

            var parser = new PDDLParser();
            var result = parser.ParseDomain(testPddl);

            if (result.Success)
            {
                var domain = result.Result;
                Debug.Log($"✓ Parsed domain: {domain.Name}");
                Debug.Log($"  Types: {domain.Types.Count}");
                Debug.Log($"  Predicates: {domain.Predicates.Count}");
                Debug.Log($"  Actions: {domain.Actions.Count}");

                // Test serialization
                string serialized = domain.ToPddl();
                Debug.Log($"✓ Serialized back to PDDL:\n{serialized}");
            }
            else
            {
                Debug.LogError($"✗ Parse failed with {result.Errors.Count} errors:");
                foreach (var error in result.Errors)
                {
                    Debug.LogError($"  Line {error.Line}:{error.Column} - {error.Message}");
                }
            }
        }
    }
}
