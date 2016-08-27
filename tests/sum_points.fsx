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


open Microsoft.FSharp.Linq.RuntimeHelpers
open System.Runtime.InteropServices

// There are some points that I don't have clear about how would be possible to create a type that represents a vector or point
// And then can be summed and do operatiosn with them
// 
// I have asked on stackoverflow and there's no valid answer yet -.-
// What I present is:
// 
// Two methods to define a structure:
//      - struc
//      - Record
//
//  The problem is that defining the sum as 
//    let sum (a,b,wi)=
//        gid, def array
//        let a = pointType(ax+bx, ay+by)
//        array.[gid] <- a
//        array
//    works
//    while
//    let sum_i(ai,bi) =
//      pointType(aix+bix, aiy+biy)
//     
//     let sum(a,b,wi)=
//        gid, def array
//        let s = sum_i(a.[gid],b.[bid])     
//        array.[gid] <- s
//        array
//
//    Doesn't work



[<StructLayout(LayoutKind.Sequential)>]
type gpu_point2 =
    struct
        val mutable x: float32
        val mutable y: float32
        [<ReflectedDefinition>] new ( q ,w) = {x=q; y=w} 
    end  
[<StructLayout(LayoutKind.Sequential)>]
type gpu_point_2 = {x:float32; y:float32} 

[<ReflectedDefinition;Kernel>]
let PointSum(a:gpu_point2,b:gpu_point2) =
     let sx =(a.x+b.x)
     let sy =(a.y+b.y)
     gpu_point2(sx,sy)       

[<ReflectedDefinition>]
let PointSum_2(a:gpu_point_2,b:gpu_point_2) =
     let sx =(a.x+b.x)
     let sy =(a.y+b.y)
     {x=sx;y= sy}   

[<ReflectedDefinition;Kernel>]
let PointSum_alternative(a:gpu_point2[],b:gpu_point2[], wi:WorkItemInfo) =
     let id = wi.GlobalID(0)
     let arp = Array.zeroCreate<gpu_point2> b.Length
     let mutable newPoint = new gpu_point2()
     let sx =(a.[id].x+b.[id].x)
     let sy =(a.[id].y+b.[id].y)
     //gpu_point2(sx,sy)       
     newPoint.x <- sx       
     newPoint.y <- sy       
     arp.[id] <- newPoint
     arp

[<ReflectedDefinition;Kernel>]
let Modgpu(b:float32[], c:float32[],wi:WorkItemInfo) =
    let gid = wi.GlobalID(0)
    let arp = Array.zeroCreate<gpu_point2> b.Length
    let newpoint = gpu_point2(b.[gid],c.[gid])
    arp.[gid] <- newpoint
    arp
[<ReflectedDefinition;Kernel>]
let Modgpu_2(b:float32[], c:float32[],wi:WorkItemInfo) =
    let gid = wi.GlobalID(0)
    let arp = Array.zeroCreate<gpu_point_2> b.Length
    let newpoint = {x=b.[gid];y=c.[gid]}
    arp.[gid] <- newpoint
    arp

[<ReflectedDefinition;Kernel>]
let ModSum(a:gpu_point2[],b:gpu_point2[],wi:WorkItemInfo) =
    let gid = wi.GlobalID(0)
    let cadd = Array.zeroCreate<gpu_point2> a.Length 
    let newsum = PointSum(a.[gid],b.[gid]) 
    cadd.[gid] <-  newsum
    cadd


[<ReflectedDefinition;Kernel>]
let ModSum_2(a:gpu_point_2[],b:gpu_point_2[],wi:WorkItemInfo) =
    let gid = wi.GlobalID(0)
    let cadd = Array.zeroCreate<gpu_point_2> a.Length 
    let newsum = PointSum_2(a.[gid],b.[gid]) 
    cadd.[gid] <-  newsum
    cadd
[<ReflectedDefinition;Kernel>]
let ModSum2(a:gpu_point2[],b:gpu_point2[],wi:WorkItemInfo) =
    let gid = wi.GlobalID(0)
    let cadd = Array.zeroCreate<gpu_point2> a.Length 
    let newsum = gpu_point2(a.[gid].x+b.[gid].x,a.[gid].y+b.[gid].y) 
    cadd.[gid] <- newsum
    cadd
[<ReflectedDefinition;Kernel>]
let ModSum2_2(a:gpu_point_2[],b:gpu_point_2[],wi:WorkItemInfo) =
    let gid = wi.GlobalID(0)
    let cadd = Array.zeroCreate<gpu_point_2> a.Length 
    let newsum = {x=a.[gid].x+b.[gid].x;y=a.[gid].y+b.[gid].y}
    cadd.[gid] <- newsum
    cadd
// Create Worksize and some points
let ws = WorkSize(64L)
let arr_s1= <@ Modgpu([|0.f..63.f|],[|63.f..(-1.f)..0.f|],ws)@>.Run()
let arr_s2 = <@ Modgpu([|63.f..(-1.f)..0.f|],[|0.f..63.f|],ws)@>.Run()

// shouldn't work
let rsum0 = <@ ModSum(arr_s1,arr_s2,ws)@>.Run()
let rsum0_1 = <@ PointSum_alternative(arr_s1,arr_s2,ws)@>.Run()
// should work
let rsum1 = <@ ModSum2(arr_s1,arr_s2,ws)@>.Run()

let aaa = <@  PointSum(rsum1.[10],rsum1.[2] )
          @>.Run()
          
          
rsum0_1 |> Array.iter(fun xx -> printfn "%+A %+A" xx.y xx.x)

// do all with record
let arr_s1_2= <@ Modgpu_2([|0.f..63.f|],[|63.f..(-1.f)..0.f|],ws)@>.Run()
let arr_s2_2 = <@ Modgpu_2([|63.f..(-1.f)..0.f|],[|0.f..63.f|],ws)@>.Run()

let rsum0_2 = <@ ModSum_2(arr_s1_2,arr_s2_2,ws)@>.Run()
