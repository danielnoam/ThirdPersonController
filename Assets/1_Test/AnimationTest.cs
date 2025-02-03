using System;
using UnityEngine;

public class AnimationTest : MonoBehaviour
{

    public float acceleration = 2.0f;
    public float deceleration = 2.0f;
    public float maxWalkVelocity = 0.5f;
    public float maxRunVelocity = 2.0f;

    private  Animator _animator;
    private int _velocityZHash;
    private int _velocityXHash;
    private float _velocityZ = 0;
    private float _velocityX = 0;

    private bool _forwardPressed;
    private bool _leftPressed;
    private bool _rightPressed;
    private bool _runPressed;
    
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _velocityZHash = Animator.StringToHash("Velocity Z");
        _velocityXHash = Animator.StringToHash("Velocity X");
    }

    private void Update()
    {
        GetInput();
        float currentMaxVelocity = _runPressed ? maxRunVelocity : maxWalkVelocity;
        
        
        // Walking
        if (_forwardPressed && _velocityZ < currentMaxVelocity)
        {
            _velocityZ += Time.deltaTime * acceleration;
        }
        
        if (_leftPressed && _velocityX > -currentMaxVelocity)
        {
            _velocityX -= Time.deltaTime * acceleration;
        }
        
        if (_rightPressed && _velocityX < currentMaxVelocity)
        {
            _velocityX += Time.deltaTime * acceleration;
        }
        
        
        // Deceleration and resets for Z
        if (!_forwardPressed && _velocityZ > 0.0f)
        {
            _velocityZ -= Time.deltaTime * deceleration;
        }
        if (!_forwardPressed && _velocityZ < 0.0f)
        {
            _velocityZ = 0.0f;
        }

        // Deceleration and reset for X
        if (!_leftPressed && _velocityX < 0.0f)
        {
            _velocityX += Time.deltaTime * deceleration;
        }
        if (!_rightPressed && _velocityX > 0.0f)
        {
            _velocityX -= Time.deltaTime * deceleration;
        }
        if (!_rightPressed && !_leftPressed && _velocityX != 0.0f && (_velocityX > -0.05f && _velocityX < 0.05f))
        {
            _velocityX = 0.0f;
        }
        
        
        // Lock forward
        if (_forwardPressed && _runPressed && _velocityZ > currentMaxVelocity)
        {
            _velocityZ = currentMaxVelocity;
        }
        else if (_forwardPressed && _velocityZ > currentMaxVelocity)
        {
            _velocityZ -= Time.deltaTime * deceleration;

            if (_velocityZ > currentMaxVelocity && _velocityZ < (currentMaxVelocity + 0.05f))
            {
                _velocityZ = currentMaxVelocity;
            }
        }
        else if (_forwardPressed && _velocityZ < currentMaxVelocity && _velocityZ > (currentMaxVelocity - 0.05f))
        {
            _velocityZ = currentMaxVelocity;
        }
        
        // Lock left/right
        if (_leftPressed && _runPressed && _velocityX < -currentMaxVelocity)
        {
            _velocityX = -currentMaxVelocity;
        }
        else if (_leftPressed && _velocityX < -currentMaxVelocity)
        {
            _velocityX += Time.deltaTime * deceleration;

            if (_velocityX < -currentMaxVelocity && _velocityX > (-currentMaxVelocity - 0.05f))
            {
                _velocityX = -currentMaxVelocity;
            }
        }
        else if (_leftPressed && _velocityX > -currentMaxVelocity && _velocityX < (-currentMaxVelocity + 0.05f))
        {
            _velocityX = -currentMaxVelocity;
        }

        if (_rightPressed && _runPressed && _velocityX > currentMaxVelocity)
        {
            _velocityX = currentMaxVelocity;
        }
        else if (_rightPressed && _velocityX > currentMaxVelocity)
        {
            _velocityX -= Time.deltaTime * deceleration;

            if (_velocityX > currentMaxVelocity && _velocityX < (currentMaxVelocity + 0.05f))
            {
                _velocityX = currentMaxVelocity;
            }
        }
        else if (_rightPressed && _velocityX < currentMaxVelocity && _velocityX > (currentMaxVelocity - 0.05f))
        {
            _velocityX = currentMaxVelocity;
        }
        
        
        // Set animator floats
        _animator.SetFloat(_velocityZHash, _velocityZ);
        _animator.SetFloat(_velocityXHash, _velocityX);
    }

    private void GetInput()
    {
        _forwardPressed = Input.GetKey(KeyCode.W);
        _leftPressed = Input.GetKey(KeyCode.A);
        _rightPressed = Input.GetKey(KeyCode.D);
        _runPressed = Input.GetKey(KeyCode.LeftShift);
        
    }
}
