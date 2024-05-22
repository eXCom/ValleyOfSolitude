using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;
    public Transform cam; // Add this line

    private bool isThirdPersonActive;

    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    public Animator playerAnim;
    public Rigidbody playerRigid;
    public float w_speed, wb_speed, olw_speed, rn_speed, ro_speed;
    public bool walking;
    public Transform playerTrans;

    // Rotation smoothing
    public float rotationSpeed = 10f;

    // Start is called before the first frame update
    void Start()
    {
        // Activate the initial camera
        ActivateCamera(firstPersonCamera);

        readyToJump = true;
        playerRigid = GetComponent<Rigidbody>();
        playerRigid.freezeRotation = true;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleCameras();
        }

        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();

        // handle drag
        if (grounded)
        {
            playerRigid.drag = groundDrag;
        }
        else
        {
            playerRigid.drag = 0;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            playerAnim.SetTrigger("walk");
            playerAnim.ResetTrigger("idle");
            walking = true;
            //steps1.SetActive(true);
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            playerAnim.ResetTrigger("walk");
            playerAnim.SetTrigger("idle");
            walking = false;
            //steps1.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            playerAnim.SetTrigger("walkback");
            playerAnim.ResetTrigger("idle");
            //steps1.SetActive(true);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            playerAnim.ResetTrigger("walkback");
            playerAnim.SetTrigger("idle");
            //steps1.SetActive(false);
        }
        if (Input.GetKey(KeyCode.A))
        {
            playerTrans.Rotate(0, -ro_speed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            playerTrans.Rotate(0, ro_speed * Time.deltaTime, 0);
        }
        if (walking == true)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                //steps1.SetActive(false);
                //steps2.SetActive(true);
                w_speed = w_speed + rn_speed;
                playerAnim.SetTrigger("run");
                playerAnim.ResetTrigger("walk");
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                //steps1.SetActive(true);
                //steps2.SetActive(false);
                w_speed = olw_speed;
                playerAnim.ResetTrigger("run");
                playerAnim.SetTrigger("walk");
            }
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();

        // Rotate player based on first-person camera orientation
        if (!isThirdPersonActive)
        {
            Vector3 cameraForward = firstPersonCamera.transform.forward;
            cameraForward.y = 0f; // Keep the player rotation only on the horizontal plane
            cameraForward = cameraForward.normalized; // Normalize the vector

            // Check if the cameraForward vector is non-zero
            if (cameraForward != Vector3.zero)
            {
                // Calculate the target rotation based on camera forward direction
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

                // Smoothly interpolate between the current rotation and target rotation
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        if (isThirdPersonActive)
        {
            // Calculate movement direction relative to the third-person camera
            Transform camTransform = thirdPersonCamera.transform;
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;

            // Ignore the vertical component of the camera direction
            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            // Update orientation to match the camera's orientation
            orientation.forward = forward;
            orientation.right = right;

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            moveDirection = cam.right * horizontalInput + cam.forward * verticalInput; // Update this line
            moveDirection.y = 0f; // Ensure we're only moving on the XZ plane
        }
        else
        {
            // Calculate movement direction
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        }

        // Apply movement force
        if (grounded)
        {
            playerRigid.AddForce(moveDirection.normalized * moveSpeed * 1000f, ForceMode.Force);
        }
        else
        {
            playerRigid.AddForce(moveDirection.normalized * moveSpeed * 1000f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(playerRigid.velocity.x, 0f, playerRigid.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            playerRigid.velocity = new Vector3(limitedVel.x, playerRigid.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        playerRigid.velocity = new Vector3(playerRigid.velocity.x, 0f, playerRigid.velocity.z);
        playerRigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void ToggleCameras()
    {
        // Switch between first-person and third-person cameras
        isThirdPersonActive = !isThirdPersonActive;
        ActivateCamera(isThirdPersonActive ? thirdPersonCamera : firstPersonCamera);
    }

    private void ActivateCamera(GameObject camera)
    {
        // Deactivate all cameras
        firstPersonCamera.SetActive(false);
        thirdPersonCamera.SetActive(false);

        // Activate the specified camera
        camera.SetActive(true);
    }
}
