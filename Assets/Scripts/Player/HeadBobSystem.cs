using UnityEngine;

namespace PlayerSystem
{
    public class HeadBobSystem : MonoBehaviour
    {
        [SerializeField, Range(0.001f, 0.01f)] float amount = 0.002f;
        [SerializeField, Range(1f, 30f)] float frequency = 10.0f;
        [SerializeField, Range(1f, 30f)] float runFrequency = 15.0f;
        [SerializeField, Range(10f, 100f)] private float smooth = 10.0f;
        [SerializeField] private float returnSpeed = 2f;

        public PlayerController playerController;
        public PlayerController PlayerController => playerController;

        private Vector3 StartPos;
        private void Start()
        {
            StartPos = transform.localPosition;
        }
        private void Update()
        {
            CheckForHeadbobTrigger();
            StopHeadbob();
        }
        private void CheckForHeadbobTrigger()
        {
            if (playerController == null) { return; }

            float inputMagnitude = new Vector3(PlayerController.MoveVector.x, 0, PlayerController.MoveVector.y).magnitude;

            if (inputMagnitude > 0)
            {
                StartHeadBob();
            }
        }
        private Vector3 StartHeadBob()
        {
            Vector3 headBobPos = Vector3.zero;

            float speedValue = PlayerController.IsSprinting ? runFrequency : frequency;

            headBobPos.y += Mathf.Lerp(headBobPos.y, Mathf.Sin(Time.time * speedValue) * amount * 1.4f, smooth * Time.deltaTime);
            headBobPos.x += Mathf.Lerp(headBobPos.x, Mathf.Cos(Time.time * speedValue / 2f) * amount * 1.6f, smooth * Time.deltaTime);

            transform.localPosition += headBobPos;

            return headBobPos;
        }
        private void StopHeadbob()
        {
            if (transform.localPosition != StartPos)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, StartPos, returnSpeed * Time.deltaTime);
            }
        }
    }
}