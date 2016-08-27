// How to reference FSCL - It's very important the order
#r @"../packages/FSCL.Compiler.2.0.1/lib/net45/FSCL.Compiler.Core.dll"
#r @"../packages/FSCL.Compiler.2.0.1/lib/net45/FSCL.Compiler.dll"
#r @"../packages/FSCL.Compiler.2.0.1/lib/net45/FSCL.Compiler.Language.dll"
#r @"../packages/FSCL.Compiler.2.0.1/lib/net45/FSCL.Compiler.NativeComponents.dll"
#r @"../packages/FSCL.Compiler.2.0.1/lib/net45/FSCL.Compiler.Util.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/FSCL.Runtime.CompilerSteps.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/FSCL.Runtime.Core.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/FSCL.Runtime.Execution.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/FSCL.Runtime.Language.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/FSCL.Runtime.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/FSCL.Runtime.Scheduling.dll"
#r @"../packages/FSCL.Runtime.2.0.1/lib/net451/OpenCLManagedWrapper.dll"

// open the namespaces
open FSCL.Compiler
open FSCL.Language
open FSCL.Runtime

//
//Here I want to sum two vectors whose length is longer than the globalsize 
//Then I iterate the the times the vector to sum is longer than the global size creating blocks 
//

let gsize = (1024*5)
let a = Array.init<float32> gsize (fun i -> (float32) i ) 
let b = Array.init<float32> gsize (fun i -> (float32) -(2*i) ) 

[<ReflectedDefinition;Kernel>]
let StrangeSum (a:float32[],b:float32[],ws:WorkItemInfo) = 
    // 
    // Ising localSize was not working

    let out_arr = Array.zeroCreate<float32> (a.GetLength(0)) 
    let size = ws.GlobalSize(0)             // block size
    let nblocks = a.GetLength(0)/size       // number of blocks

    let gid = ws.GlobalID(0)

    for i in 0.. (nblocks-1) do
        let localsum = (a.[gid+i*size] + b.[gid+i*size])
        out_arr.[gid+i*size] <- localsum//a.[gid+i*size]+ b.[gid+i*size]

    out_arr

// define the worksize
let wi = WorkSize(1024L)

// Perfom the sum by blocks
let c= <@StrangeSum(a,b,wi)@>.Run() //|> fun x -> x.Length/1024//|> Array.filter(fun x -> x<>0.f)

c.[5080..c.Length-1]