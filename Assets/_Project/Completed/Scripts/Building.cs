using UnityEngine;

namespace InfallibleCode.Completed
{
    public class Building : MonoBehaviour
    {
        [SerializeField] private int floors;

        public struct Data
        {
            private int _tenants;
            
            public int PowerUsage { get; private set; }

            private Unity.Mathematics.Random _random;

            public Data(Building building)
            {
                _random = new Unity.Mathematics.Random(1);
                _tenants = building.floors * _random.NextInt(20, 500);
                PowerUsage = 0;
            }

            public void Update()
            {
                var random = new Unity.Mathematics.Random(1);
                for (var i = 0; i < _tenants; i++)
                {
                    PowerUsage += random.NextInt(12, 24);
                }
            }
        }
    }
}