using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StickyPhysics
{

    public class AfterUnityPhysics : MonoBehaviour
    {
        static bool initialized = false;
        static List<StickyRigidBody> stickyRBs = new List<StickyRigidBody>();
        float t = 0;

        // Initializes the two objects needed to generate event after unity physics runs
        public static void init()
        {
            
            if (!initialized)
            {
                GameObject obj1 = new GameObject("StickyPhysicsUtilityObj");
                GameObject obj2 = new GameObject("StickyPhysicsUtilityObj");
                obj1.AddComponent<SphereCollider>().isTrigger = true;
                obj2.AddComponent<SphereCollider>();
                Rigidbody r1 = obj1.AddComponent<Rigidbody>();
                r1.useGravity = false;
                r1.isKinematic = true;
                obj1.AddComponent<AfterUnityPhysics>();
                obj1.layer = 24;
                obj2.layer = 24;
            }
        }

        public static void add(StickyRigidBody obj)
        {
            stickyRBs.Add(obj);
        }

        public static void remove(StickyRigidBody obj)
        {
            stickyRBs.Remove(obj);
        }

        // Evoke afterphysics event
        private void OnTriggerStay(Collider other)
        {
            for (int i = 0; i < stickyRBs.Count; i++)
            {
                stickyRBs[i].AfterPhysicsUpdate();
            }
        }
    }
}