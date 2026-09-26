using UnityEngine;

namespace PlayerSystem
{
    public class CameraTilt : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [Space]
        [SerializeField] private float maxAngle = 10f;
        [SerializeField] private float tiltSpeed = 2f;

        private float rotX = 0f;
        private float rotZ = 0f;

        private void Update()
        {
            Tilting();
        }
        private void Tilting()
        {
            float targetRotX = controller.CurrentInputVectorInt.y != 0f ? maxAngle * Mathf.Sign(controller.CurrentInputVectorInt.y) : 0f;
            float targetRotZ = controller.CurrentInputVectorInt.x != 0f ? maxAngle * Mathf.Sign(controller.CurrentInputVectorInt.x) : 0f;

            rotX = Mathf.Lerp(rotX, targetRotX, Time.deltaTime * tiltSpeed);
            rotZ = Mathf.Lerp(rotZ, targetRotZ, Time.deltaTime * tiltSpeed);

            transform.localRotation = Quaternion.Euler(rotX, 0f, -rotZ);
        }
    }
}
