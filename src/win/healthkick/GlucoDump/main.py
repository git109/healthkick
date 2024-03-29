#!/usr/bin/env python

import sys
#sys.path.append("c:\\workspace\\healthkick\\src\\win\\healthkick\\GlucoDump")
#sys.path.append("C:\\Python26\\Lib\\site-packages")
#sys.path.append("C:\\Python26\\Lib")
print sys.path

import usbcomm, contourusb

def main(argv):
    uc = usbcomm.USBComm(idVendor=usbcomm.ids.Bayer,
                         idProduct=usbcomm.ids.Bayer.Contour)
    bc = contourusb.BayerCOMM(uc)
    cu = contourusb.ContourUSB()

    for rec in bc.sync():
        cu.record(rec)

    for resno, res in cu.result.items():
        print '%s: %s %.1f %s %s' % (resno, res.testtime, res.value, res.unit,
                               ', '.join(res.resultflags))

if __name__ == '__main__':
    import sys
    main(sys.argv)
