using Godot;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Tools
{
    public class MoveHelper
    {
        private const float RunInputThreshold = 0.7f;

        public float WalkSpeed { get; set; }

        public float RunSpeed { get; set; }
        
        public float Acceleration { get; set; }

        public float RunTime { get; private set; }

        public readonly IPhysicsEntity Entity;
        private float _delta;
        
        public MoveHelper(IPhysicsEntity entity, float walkSpeed, float runSpeed, float acceleration)
        {
            Entity = entity;
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            Acceleration = acceleration;
        }

        public void Update(float delta, float input)
        {
            _delta = delta;
            if (Mathf.Abs(input) > RunInputThreshold)
                RunTime += delta;
            else
                RunTime = 0.0f;
            
            float topSpeed = IsRunning ? RunSpeed : WalkSpeed;
            
            Vector3 velocity = Entity.Velocity;
            velocity.x = Mathf.MoveToward(velocity.x, input * topSpeed, Acceleration * delta);
            Entity.Velocity = velocity;
        }

        public void Reset() => RunTime = 0.0f;

        public bool IsRunning => RunTime > Acceleration * _delta;
    }
}