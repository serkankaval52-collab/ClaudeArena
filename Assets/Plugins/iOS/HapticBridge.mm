#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *g_lightGenerator  = nil;
static UIImpactFeedbackGenerator *g_mediumGenerator = nil;
static UIImpactFeedbackGenerator *g_heavyGenerator  = nil;
static UIImpactFeedbackGenerator *g_softGenerator   = nil;
static UIImpactFeedbackGenerator *g_rigidGenerator  = nil;
static BOOL g_hapticsSupported = NO;
static BOOL g_initialized      = NO;

static BOOL DeviceSupportsHaptics(void)
{
    if (@available(iOS 10.0, *)) {
        return NSClassFromString(@"UIImpactFeedbackGenerator") != nil;
    }
    return NO;
}

static void EnsureInitialized(void)
{
    if (g_initialized) return;
    g_initialized = YES;
    g_hapticsSupported = DeviceSupportsHaptics();
    if (!g_hapticsSupported) return;

    if (@available(iOS 10.0, *)) {
        g_lightGenerator  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        g_mediumGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        g_heavyGenerator  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
    }
    if (@available(iOS 13.0, *)) {
        g_softGenerator  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleSoft];
        g_rigidGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleRigid];
    }
}

static UIImpactFeedbackGenerator *GeneratorForStyle(int style)
{
    switch (style) {
        case 0: return g_lightGenerator;
        case 1: return g_mediumGenerator;
        case 2: return g_heavyGenerator;
        case 3: return g_softGenerator  ?: g_mediumGenerator;
        case 4: return g_rigidGenerator ?: g_mediumGenerator;
        default: return g_mediumGenerator;
    }
}

extern "C" {
    void _PrepareiOSHaptic(int style)
    {
        EnsureInitialized();
        if (!g_hapticsSupported) return;
        [GeneratorForStyle(style) prepare];
    }

    void _PlayiOSHapticImpact(int style)
    {
        EnsureInitialized();
        if (!g_hapticsSupported) return;
        [GeneratorForStyle(style) impactOccurred];
    }
}
