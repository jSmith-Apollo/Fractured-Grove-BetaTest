using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ClimbAndCling : MonoBehaviour
{
    [Header("Layer")]
    public LayerMask WhatIsWall;
    public LayerMask WhatIs4DWall;
    public LayerMask WhatIsWallInBoth;
    public LayerMask WhatIsWallAndPT;

    [Header("Climbing variables")]
    private bool canCling;
    public float climbTime;
    public float KickOffStrength;
    private bool Debounce;
    public float ClingCooldown;

    private IEnumerator ClingReset;

    [Header("WallRun Info")]
    private bool IsWallRunning;
    private bool WallRunDirRight; //True == Right, False == Left 
    private bool canWallRunRight;
    private bool canWallRunLeft;
    public float WallRunTime;
    public float minWallRunSpeed;
    public float wallRunCharAngle;
    private float CharStartAngle;

    [Header("PlayerInfo")]
    public PlayerMovement Mover;
    public Transform orientation;
    public Rigidbody rb;

    [Header("DebugOptions")]
    public bool debugMode;
    public float WallRunRadius;
    public float WallRunMaxDistance;
    public float ClingRadius;
    public float ClingMaxDistance;
    public float KickOffRadius;
    public float KickOffMaxDistance;

    // Start is called before the first frame update
    void Start()
    {
        ClingReset = StopCling(0f);
        canCling = true;
        canWallRunLeft = true;
        canWallRunRight = true;
        Debounce = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Check if player is able to cling
        if (Input.GetKey(Mover.jumpKey) && ReadyToCling() && canCling)
        {
            print("readyToCling");
            canCling = false;
            Debounce = true;
            cling();

            //Reset debounce
            Invoke(nameof(ResetDebounce), 0.5f);
        }
        //Check if player is able to climnb
        else if (Input.GetKey(Mover.jumpKey) && Mover.state == PlayerMovement.MovementState.clinging && ((Physics.SphereCast(transform.position, 0, orientation.forward, out RaycastHit hitInfo,2, WhatIsWall) || ((Physics.SphereCast(transform.position, 0, orientation.forward, out hitInfo, 2, WhatIs4DWall) && Mover.nav.IsIn4D()) || ((Physics.SphereCast(transform.position, 0, orientation.forward, out hitInfo, 2, WhatIsWallAndPT)) && !Mover.nav.IsIn4D()) || (Physics.SphereCast(transform.position, 0, orientation.forward, out hitInfo, 2, WhatIsWallInBoth)))) && !Debounce))
        {
            Climb();
        }
        //Check if wall is behind to KickOff
        else if (Input.GetKey(Mover.jumpKey) && (Mover.state == PlayerMovement.MovementState.clinging || Mover.state == PlayerMovement.MovementState.climbing) && Physics.SphereCast(transform.position, 0, orientation.forward * -1, out RaycastHit hitInfo2, 3, WhatIsWall) && !Debounce)
        {
            KickOff();
        }
        else if (Input.GetKey(Mover.jumpKey) && (Mover.state == PlayerMovement.MovementState.walking || Mover.state == PlayerMovement.MovementState.clinging) && !Mover.GetGrounded() && ReadyToWallRun(true) && !Debounce)
        {
            WallRun(true);
        }
        //Check for wall run to the left
        else if (Input.GetKey(Mover.jumpKey) && (Mover.state == PlayerMovement.MovementState.walking || Mover.state == PlayerMovement.MovementState.clinging) && !Mover.GetGrounded() && ReadyToWallRun(false) && !Debounce)
        {
            print("wallRunLeft");
            WallRun(false);
        }

        if (Mover.state == PlayerMovement.MovementState.clinging)
        {
            rb.velocity = new Vector3(0,-1,0); 
        }
        else if (Mover.state == PlayerMovement.MovementState.climbing)
        {
            rb.velocity = new Vector3(0, 3, 0);
        }
        else if (IsWallRunning)
        {
            rb.velocity = new Vector3(rb.velocity.x, -0.5f, rb.velocity.z);
            //check if the user has no wall in their wallrun direction
            if ((WallRunDirRight && !Physics.SphereCast(orientation.position, 0.4f, orientation.right, out RaycastHit hitinfo, 0.5f, WhatIsWall)) || (!WallRunDirRight && !Physics.SphereCast(orientation.position, 0.4f, orientation.right * -1, out RaycastHit hitinfo2, 0.5f, WhatIsWall)))
            {
                print("stopped");
                CancelInvoke();
                StopWallRun();
            }
        }

        //Cooldown Resets//
        if (Mover.GetGrounded() && !canCling)
        {
            StartCoroutine(StopCling(2.3f));
            Invoke(nameof(ResetCooldown), ClingCooldown);
        }
        if (Mover.GetGrounded())
        {
            canWallRunLeft = true;
            canWallRunRight = true;
        }
    }

    private void cling()
    {
        Mover.state = PlayerMovement.MovementState.clinging;
        Mover.SetMoveSpeed(2);
        StartCoroutine(ClingReset);
        
    }
 
    private void KickOff()
    {
        print("KickOff");
        Mover.state = PlayerMovement.MovementState.air;
        StopCoroutine(ClingReset);
        Mover.SetMoveSpeed(KickOffStrength*2);
        rb.velocity = orientation.forward * KickOffStrength * 10f + orientation.up * KickOffStrength/5 ;

    }
    private void Climb()
    {
        print("climbing");
        Mover.state = PlayerMovement.MovementState.climbing;
        StopCoroutine(ClingReset);


        Invoke(nameof(StopClimb), climbTime);
    }

    private void WallRun(bool IsRight)
    {
        canCling = false;
        IsWallRunning = true;
        rb.useGravity = false;
        canWallRunRight = false;
        canWallRunLeft = false;

        if (Mover.state == PlayerMovement.MovementState.clinging)
        {
            Mover.state = PlayerMovement.MovementState.air;
            StopCoroutine(ClingReset);
        }

        if (IsRight)
        {
            print("wallRunRight");
            WallRunDirRight = true;
        }
        else
        {
            print("wallRunLeft");
            WallRunDirRight = false;
        }

        Invoke(nameof(StopWallRun), WallRunTime);
    }


    private void StopClimb()
    {
        Mover.state = PlayerMovement.MovementState.air;
        Mover.SetMoveSpeed(2);
        Invoke(nameof(ResetCooldown), ClingCooldown);
    }

    private IEnumerator StopCling(float t)
    {
        yield return new WaitForSeconds(t);
        print("clingEnded");
        Mover.state = PlayerMovement.MovementState.air;
        Mover.SetMoveSpeed(1);

 }

    private void StopWallRun()
    {
        //push player down//
        rb.AddForce(orientation.up * -1 * 20, ForceMode.Force);
        rb.useGravity = true;
        IsWallRunning = false;
        if (WallRunDirRight)
        {
            canWallRunLeft = true;
            Invoke(nameof(ResetWallRunCooldownRight), 0.5f);

        }
        else
        {
            canWallRunRight = true;
            Invoke(nameof(ResetWallRunCooldownLeft), 0.5f);

        }
    }

    public void ResetCooldown()
    {
        print("ResetCooldown");
        canCling = true;
    }

    public void ResetDebounce()
    {
        Debounce = false;
    }

    public void ResetWallRunCooldownRight()
    {
        canWallRunRight = true;
    }

    public void ResetWallRunCooldownLeft()
    {
        canWallRunLeft = true;
    }

    public bool ReadyToCling()
    {
        return Physics.SphereCast(transform.position, 0, orientation.forward, out RaycastHit hitInfo, 1, WhatIsWall) && Mover.state != PlayerMovement.MovementState.clinging;
    }

    public bool ReadyToWallRun(bool toRight)
    {
        if (toRight)
        {
            //print(canWallRun && Mover.GetMoveSpeed() >= minWallRunSpeed && Physics.SphereCast(orientation.position, 0.4f, orientation.right, out RaycastHit hitinfotest, 0.5f, WhatIsWall));
            WallRunDirRight = true;
            return canWallRunRight && Mover.GetMoveSpeed() >= minWallRunSpeed && Physics.SphereCast(orientation.position, 0.4f, orientation.right, out RaycastHit hitinfo, 0.5f, WhatIsWall);
        }
        else
        {
            //print(canWallRun && Mover.GetMoveSpeed() >= minWallRunSpeed && Physics.SphereCast(orientation.position, 0.4f, orientation.right * -1, out RaycastHit hitinfotest, 0.5f, WhatIsWall));
            WallRunDirRight = false;
            return canWallRunLeft && Mover.GetMoveSpeed() >= minWallRunSpeed && Physics.SphereCast(orientation.position, 0.4f, orientation.right * -1, out RaycastHit hitinfo, 0.5f, WhatIsWall);
        }
    }

    private void OnDrawGizmos()
    {
        if (debugMode)
        {
            Gizmos.color = Color.red;
            //Wall Run Checks
            Gizmos.DrawSphere(orientation.position + orientation.right * WallRunMaxDistance * -1, WallRunRadius);
            Gizmos.DrawSphere(orientation.position + orientation.right * WallRunMaxDistance, WallRunRadius);
            //Climb&ClingCheck
            Gizmos.DrawSphere(orientation.position + orientation.forward * ClingMaxDistance, ClingRadius);
            //KickOffCheck//
            Gizmos.DrawSphere(orientation.position + orientation.forward * KickOffMaxDistance * -1, KickOffRadius);

        }
    }
}