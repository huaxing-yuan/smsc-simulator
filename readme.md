# SMS-C Simulator

This extension of Hummingbird Test Framework implements a portion of UCP-EMI protocol, it can be used as a SMS-C simulator to receive MT messages.
The SMS-C simulator can also sent MT Acknowledges, and SR Messages automatically.

When a SMS-C client is connected, you can also sent MO Messages to the socket client.

While this extension is installed to Hummingbird App, you can find:
1. An SMSC Server available in the list of services available
2. A Custom Message Viewer, automatically shown when you want to view a received message.
3. MO, SR requests can be sent through socket when a client is connected. (When multiple socket clients are connected, MO and SR are always sent to the first client)

master: ![master](https://hummingbird.visualstudio.com/Hummingbird%20ALM/_apis/build/status/Hummingbird%20Extension%20SMS-C)

## Aimed for demonstration
This extension is aimed to demonstrate the extensibility of Hummingbird Test Framework:
1. By implementing a Non HTTP protocol (UCP-EMI protocol is based on TCP socket)
2. By implementing a custom message viewer to visualize non text message
Fill free to use this extension (if you are working in a telecom operation) with hummingbird application. (http://www.hummingbird-alm.com)

## Remake
The extension is build from the existing code of my previous project from 2010 to 2012 and adapted to run on Hummingbird Test Framework. The code is partially built on ALAZ socket library. Not all services of UCP-EMI protocol are implemented, the implemented services are:
UCP 60: Session
UCP 51: MT message
UCP 52: MO Message
UCP 53: Delivery Report

## Licenses
You can use these source code to:
 - Demonstrate Hummingbird and Hummingbird Test Framework
 - Use this extension with Hummingbird / Hummingbird Test Framework within your organization for test.
 - Improve the extension, implement more UCP operations and use it with Hummingbird / Hummingbird Test Framework.
 - All modifications of code must ensure that the code is aimed to running on Hummingbird Test Framework.

 You must not:
 - Grab the source code, use any part of them to build another software instead of using Hummingbird Test Framework
 - Sell this extension to a 3rd party person or organization

## Disclaimer
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
