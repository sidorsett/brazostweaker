## Introduction ##
BrazosTweaker is a tool for undervolting and underclocking AMD Brazos platform (Zacate E-350, E-450 / Ontario C-50, C-60, C-70, E2-1800) APU's under Windows 7 (XP/Vista). By using it, you can achieve longer batterylife (lower power consumption) as well as lower processor temperatures.

Original version of the tool hosted at [Google Code](https://code.google.com/archive/p/brazostweaker/) has been created Martin Kinkelin and Sven Wittek. The successor version published here improves NorthBridge undervolting capabilities of the original tool:
- The original version of the tool had an error in its service implementation. Unlike the frontend application, the service missed to set up all necessary CPU registers, which prevented NB VID settings from taking effect after startup or resume. This had effect on both NB states (NBP1 as well as NBP0).
- The original version of the tool as well as original BIOS implementations incorrectly applied voltage settings of NB P1 state, which effectively made NBP1 to inherit NBP0 VID.

The improved version available here is fully inline with BKDG (BIOS and Kernel Developer's Guide) for AMD Family 14h, but is tested only on E-350 APU. Considering that all other APUs listed above belong to the same CPU family 14h, the improvements are expected to work on the rest of them as well. Notice that the original version could allow you to go very low with undervolting NBP1 state, because in reality it was faky. Using the same NBP1 settings with the new version may cause your system to crash. Basically, you need to find out valid NBP1 voltage settings as described below from scratch. To confirm improvements over the original settings you can compare tempeartures of your system against setup with the original tool (this is actually how I reverse engineered APU behavior myself). Don't forget to fix fan speed for the duration of your temperature tests.

## Installation ##
To use the tool and it's built-in service, you need to obviously install the given files from the Download section. It could be, that you need to download and install Microsofts .NET Framework 4 upfront, but only in case, it can't be found.
If you already have a previous version of the BrazosTweaker (OntarioTweaker or PhenomMsrTweaker) installed, I strongly suggest to uninstall it first.

To install BrazosTweaker in Windows 8, you need to enable the .NET Runtime 2.0. You do that in the "Enable/Disable Windows features" that you'll find in "Programs and Features"

## Tabs ##
Once it has started up, you will see a window six different tabs. The three tabs on the left belong to states, which the CPU can be in, while the next two tabs show the setup for the two available NB (Northbridge) PStates. The last, but not least, is the status tab, which can be used for debug and more detailed info on the internal registers.

## CPU PStates ##
Let's start with the first three tabs.
If you use the E-350 APU, your system uses all three PStates, whereas on the C-50 only two of them are used.
You can see the current active PState by watching core progress bars followed by labels. The available PStates are called P0 / P1 / P2. PState P0 is always the fastest mode, while P2 (E-350) is the slowest one. Depending on the current workload of your system, it will switch dydmaically between them. As you can imagine, the P0 is drawing more current than P1 and P2, which is directly related to battery life you can get.

### Voltage ###
With the tool, you can now customize the voltage, which goes into your CPU (CPU VID) for each of the available PStates. Furthermore you can change the divider to get to a different CPU clock than stock.

You can start with the following changes (these are working for most of the users)
  * P0 - 1.225V
  * P1 - 1.05V
  * P2 - 0.85V

If you click "Apply" on the below right, these settings will be temporary set, until you restart or put your system into sleep. You can check the settings by using a well known tool called CPU-Z.

### Clock/Divider ###
If you want, you can play around with the dividers to adjust used clocks for each PState. For example, I let my system run for a while on 400MHz, while lowering the voltage to about 0.7V.
Unfortunately you can't overclock by using lower dividers, since the CPU seems to internally lock them to a specific value.
  * Example:
  * E-350 - 1.6GHz
  * FSB 100MHz
  * Multiplier 32x
  * Divider 2
  * Maximum clock 32/2\*100MHz = 1600MHz

Even though you can select a divider smaller than 2 on an E-350, the CPU blocks that and just runs still at 1.6GHz!

## NB PStates ##

### Voltage ###
Because the NB (Northbridge) and the GPU (graphic unit) are sharing a separate powersupply, it is worth looking into lowering this voltage in addition to gain some more battery life.
Basically the adjustments you can do, need to be done the same way as for the CPU, except that only two PStates are available.
On my system, the following settings work stable:
  * NB P0 - 0.85V
  * NB P1 - 0.8V

Just a little lower and it hangs. Just press the Power button for a while and it will get back to life.

### Clock/Divider ###
There is no opportunity to mess with the dividers on the NB so far.

## Testing ##
If everything looks alright after you apply new settings, you can download FurMark, which is program to do some stress testing. Doing that, tells you, if the system is really stable in most cases. It avoids getting blue screens or freezes later, which can be really annoying.

One should stress each of the P-states and NB P-separately. This can be achieved by modifying the selecting specific Windows power options:
  1. Go to "Power Options" in the Control Panel. 
  1. In the "Power Options" snap-in, click "Change plan settings" against your active power plan.
  1. In the "Edit Plan Settings" snap-in, click "Change advanced power settings".
  1. To limit transitions between P-states, tweak "Minimum processor state" and "Maximum processor state" under "Processor power management" branch in the newly opened "Power Options" dialog window. 
  1. To limit transitions between NB P-states, tweak "ATI Powerplay Settings" under "ATI Graphics Power Settings" branch in the same dialog window.

Just check, while running the stress test, where the asterisk is! If you want to test P1, the asterisk, should never go to P0, but while stopping the test, it can go to P2.

In case, the systems hangs during testing, you know the selected voltage was too low for that PState. Just simply hold the power button for a few second and your system restarts without the modified voltages.
After restart increase voltage a bit and start stressing the system again. I strongly suggest, not to use a voltage for later use, which is just one step away from, where it hung up. Please add about 25mV at least to have some margin.


## Service ##
### Recovery plan ###
You will need to follow these steps to disable the service in case your system stops to boot into Windows:
  1. In order to disable the service, you need to boot Windows in "Safe mode". While doing this, the BrazosTweaker service doesn't get enabled.
  1. If Windows has booted succesfully in "Safe mode", you can click on the Windows-Start button and enter "Services" in the below line. Hit "Enter".
  1. Now you can see all installed services and should look for "BrazosTweaker service".
  1. The next thing is clicking right on the line from BrazosTweaker and select "Properties".
  1. On the "Geberal" tab change "Startup type" from "Automatic" to "Manual".
  1. Now you can restart your system.
  1. Once Windows has been restarted, start BrazosTweaker and try to find the rootcause of the hangs (too low voltage for a certain PState?, bug in the code, etc.).
  1. If the problem was found and fixed, you can re-enable the service by starting with 2. and switching back to "Automatic".

### Enable service ###
After you found out stable configuration, stress-tested your system with custom voltage and clock settings, ensured everything runs smoothly, familarized yourself with the recovery plan, **and only in that case**, you can click on the "Service..." button. By setting that up, there will be a service in the background, which applies the adjusted voltages after coming out of sleep or while booting Win.

You just need to click "Update" to get the current settings, check the selection box "Make custom P-state settings permanent" and hit "Apply".
Now you are done.
