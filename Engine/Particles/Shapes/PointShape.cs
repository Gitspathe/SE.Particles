using SE.Core.Extensions;
using SE.Utility;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace SE.Particles.Shapes
{
    public class PointShape : IIntersectable
    {
        public Vector2 Center { get; set; }
        public float Rotation { get; set; }

        public bool Intersects(Vector2 point)
            => point == Center;

        public bool Intersects(Vector4 bounds)
            => bounds.Intersects(Center);
    }

    public class PointEmitterShape : PointShape, IEmitterShape
    {
        public void Get(float uniformRatio, FRandom random, out Vector2 position, out Vector2 velocity)
        {
            position = Vector2.Zero;
            random.NextUnitVector(out velocity);
        }
    }
}
