namespace AccessControl.Domain.Enums;

[Flags]
public enum DeviceFeatures
{
    None = 0,
    CardReader = 1 << 0,      // 1
    Keypad = 1 << 1,          // 2
    Biometrics = 1 << 2,      // 4
    Camera = 1 << 3,          // 8
    LockControl = 1 << 4,     // 16
    Display = 1 << 5,         // 32
    Intercom = 1 << 6,        // 64
    MotionSensor = 1 << 7     // 128
}
