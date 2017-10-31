using System;

namespace TrueSync
{

    /**
    *  @brief Represents few information about a raycast hit. 
    **/
    public class TSRaycastHit
	{
		public TSRigidBody rigidbody { get; private set; }
		public TSCollider collider { get; private set; }
		public TSTransform transform { get; private set; }
		public TSVector point { get; private set; }
		public TSVector normal { get; private set; }
		public FP distance { get; private set; }

		public TSRaycastHit(TSRigidBody rigidbody, TSCollider collider, TSTransform transform, TSVector normal, TSVector origin, TSVector direction, FP fraction)
		{
			this.rigidbody = rigidbody;
			this.collider = collider;
			this.transform = transform;
			this.normal = normal;
			this.point = origin + direction * fraction;
			this.distance = fraction * direction.magnitude;
		}
	}
}

