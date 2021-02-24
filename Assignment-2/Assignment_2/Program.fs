﻿module Assignment_2.Program

// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System

// Define a function to construct a message to print
// T-501-FMAL, Spring 2021, Assignment 2

(*
STUDENT NAMES HERE: ...
Loki Alexander Hopkins
Ríkharður Friðgeirsson

*)


(* Various type and function definitions, do not edit *)

type iexpr =
    | IVar of string
    | INumI of int
    | INumF of float
    | IPlus of iexpr * iexpr
    | ITimes of iexpr * iexpr
    | INeg of iexpr
    | IIfPositive of iexpr * iexpr * iexpr

type expr =
    | Var of string
    | NumI of int
    | NumF of float
    | Plus of expr * expr
    | Times of expr * expr
    | Neg of expr
    | IfPositive of expr * expr * expr
    | IntToFloat of expr
    | Match of expr * string * expr * string * expr

type value =
    | I of int
    | F of float

type envir = (string * value) list

type typ =
    | Int
    | Float

type tyenvir = (string * typ) list

let rec lookup (x : string) (env : (string * 'a) list) : 'a =
    match env with
    | []          -> failwith (x + " not found")
    | (y, v)::env -> if x = y then v else lookup x env

let paren b s = if b then  "(" + s + ")" else s

let iprettyprint (e : iexpr) : string =
    let rec iprettyprint' e acc =
        match e with
        | IVar x -> x
        | INumI i -> string i
        | INumF f -> sprintf "%A" f
        | IPlus  (e1, e2) ->
              paren (4 <= acc) (iprettyprint' e1 3 + " + " + iprettyprint' e2 4)
        | ITimes (e1, e2) ->
              paren (7 <= acc) (iprettyprint' e1 6 + " * " + iprettyprint' e2 7)
        | INeg e ->
              paren (10 <= acc) ("-" + iprettyprint' e 9)
        | IIfPositive (e, et, ef) ->
              paren (2 <= acc) ("if " + iprettyprint' e 3 + " > 0 then " + iprettyprint' et 2 + " else " + iprettyprint' ef 1)
    iprettyprint' e 0

let prettyprint (e : expr) : string =
    let rec prettyprint' e acc =
        match e with
        | Var x -> x
        | NumI i -> string i
        | Plus  (e1, e2) ->
             paren (4 <= acc) (prettyprint' e1 3 + " + " + prettyprint' e2 4)
        | Times (e1, e2) ->
             paren (7 <= acc) (prettyprint' e1 6 + " * " + prettyprint' e2 7)
        | Neg e ->
             paren (10 <= acc) ("-" + prettyprint' e 9)
        | IfPositive (e, et, ef) ->
             paren (2 <= acc) ("if " + prettyprint' e 3 + " > 0 then " + prettyprint' et 2 + " else " + prettyprint' ef 1)
        | NumF f -> sprintf "%A" f
        | IntToFloat e ->
             paren (10 <= acc) ("float " + prettyprint' e 10)
        | Match (e, xi, ei, xf, ef) ->
             paren (2 <= acc) ("match " + prettyprint' e 1 + " with"
               + " I " + xi + " -> " + prettyprint' ei 2
               + " | F " + xf + " -> " + prettyprint' ef 1)
    prettyprint' e 0

let plus_value (v1 : value, v2 : value) : value =
    match v1, v2 with
    | I x1, I x2 -> I (x1 + x2)
    | F x1, I x2 -> F (x1 + float x2)
    | I x1, F x2 -> F (float x1 + x2)
    | F x1, F x2 -> F (x1 + x2)

let times_value (v1 : value, v2 : value) : value =
    match v1, v2 with
    | I x1, I x2 -> I (x1 * x2)
    | F x1, I x2 -> F (x1 * float x2)
    | I x1, F x2 -> F (float x1 * x2)
    | F x1, F x2 -> F (x1 * x2)

let neg_value (v : value) : value =
    match v with
    | I x -> I (-x)
    | F x -> F (-x)

let is_positive_value (v : value) : bool =
    match v with
    | I x -> x > 0
    | F x -> x > 0.

type rinstr =
    | RLoad of int            // load from environment
    | RStore                  // move value from top of stack to
                              // 0th pos of the environment,
                              // shifting all others down
    | RErase                  // remove 0th value from environment,
                              // shifting all others up
    | RNum of int
    | RAdd
    | RSub
    | RMul
    | RPop
    | RDup
    | RSwap

type rcode = rinstr list
type stack = int list          // intermediate values
type renvir = int list         // values of numbered variables

let rec reval (inss : rcode) (stk : stack) (renv : renvir) =
    match inss, stk with
    | [], i :: _ -> i
    | [], []     -> failwith "reval: No result on stack!"
    | RLoad n :: inss,             stk ->
          reval inss (List.item n renv :: stk) renv
    | RStore  :: inss,        i :: stk -> reval inss stk (i :: renv)
    | RErase  :: inss,             stk -> reval inss stk (List.tail renv)
    | RNum i  :: inss,             stk -> reval inss (i :: stk) renv
    | RAdd    :: inss, i2 :: i1 :: stk -> reval inss ((i1+i2) :: stk) renv
    | RSub    :: inss, i2 :: i1 :: stk -> reval inss ((i1-i2) :: stk) renv
    | RMul    :: inss, i2 :: i1 :: stk -> reval inss ((i1*i2) :: stk) renv
    | RPop    :: inss,        i :: stk -> reval inss stk renv
    | RDup    :: inss,        i :: stk -> reval inss ( i ::  i :: stk) renv
    | RSwap   :: inss, i2 :: i1 :: stk -> reval inss (i1 :: i2 :: stk) renv
    | _ -> failwith "reval: too few operands on stack"


// Problem 1


let rec lookup_var (x : string) (env : (string * 'a) list) : 'a =
    match env with
    | []          -> I 0
    | (y, v)::env -> if x = y then v else lookup_var x env
let rec ieval (e : iexpr) (env : envir) : value =
    match e with
    | IVar x -> lookup_var x env                       // to modify
    | INumI i -> I i
    | INumF f -> F f
    | IPlus (e1, e2) -> plus_value (ieval e1 env, ieval e2 env)
    | ITimes (e1, e2) -> times_value (ieval e1 env, ieval e2 env)
    | INeg e -> neg_value (ieval e env)
    | IIfPositive (e, et, ef) ->
        if is_positive_value (ieval e env)
        then ieval et env
        else ieval ef env

printfn "%A" (ieval (IVar "x") [])
// val it : value = I 0
printfn "%A" (ieval (IVar "x") ["x", I 5])
// val it : value = I 5
printfn "%A" (ieval (IPlus (IVar "x", ITimes (IVar "y", IVar "z"))) ["x", F 1.1; "z", I 10])
// val it : value = F 1.1



// Problem 2

let rec eval (e : expr) (env : envir) : value =
    match e with
    | Var x -> lookup_var x env
    | NumI i -> I i
    | NumF f -> F f
    | Plus (e1, e2) ->                             // to complete
        match eval e1 env, eval e2 env with
        | I i1, I i2 -> I (i1 + i2)
        | F f1, F f2 -> F (f1 + f2)
        | _ -> failwith "wrong operand type"
    | Times (e1, e2) ->                            // to complete
        match eval e1 env, eval e2 env with
        | I i1, I i2 -> I (i1 * i2)
        | F f1, F f2 -> F (f1 * f2)
        | _ -> failwith "wrong operand type"
    | Neg e ->                                     // to complete
        match eval e env with
        | I i -> I (- i)
        | F f -> F (- f)
        | _ -> failwith "wrong operand type"
    | IntToFloat e ->
        match eval e env with
        | I i -> F (float i)
        | _ -> failwith "wrong operand type"
    | IfPositive (e, et, ef) ->
        if is_positive_value (eval e env)
        then eval et env
        else eval ef env
    | Match (e, xi, ei, xf, ef) ->
        match eval e env with
        | I n -> eval ei [xi, I n]
        | F n -> eval ef [xf, F n]
  

        
printfn "Expected F 3.3, got %A" (eval (Plus (Var "x", Var "y")) ["x", F 1.1; "y", F 2.2])
printfn "Expected F 6.05, got %A" (eval (Times (Var "x", Plus (NumF 3.3, Var "y"))) ["x", F 1.1; "y", F 2.2])
printfn "Expected F 11.6, got %A" (eval (Plus (IntToFloat (Plus (NumI 2, NumI 3)), NumF 6.6)) [])

//printfn "Expected System.Exception: wrong operand type, got %A" (eval (Plus (NumI 1, NumF 2.0)) [])
//
//printfn "Expected System.Exception: wrong operand type, got %A" (eval (Times (NumF 1.0, NumI 2)) [])

printfn "Expected F -5.6, got %A" (eval (Neg (Var "x")) ["x", F 5.6])

printfn "Expected F 1.1, got %A" (eval (IfPositive (Var "x", NumF 1.1, NumF 2.2)) ["x", I 1])
printfn "Expected F 2.2, got %A" (eval (IfPositive (Var "x", NumF 1.1, NumF 2.2)) ["x", I -1])

printfn "Expected F 1.1, got %A" (eval (IfPositive (Var "x", NumF 1.1, NumF 2.2)) ["x", F 1.0])

printfn "Expected F 2.2, got %A" (eval (IfPositive (Var "x", NumF 1.1, NumF 2.2)) ["x", F -1.0])

printfn "Expected I 12, got %A" (eval (Match (Var "x", "zi", Plus (Var "zi", NumI 2), "zf", Plus (Var "zf", NumF 3.))) ["x", I 10])

printfn "Expected F 13.0, got %A" (eval (Match (Var "x", "zi", Plus (Var "zi", NumI 2), "zf", Plus (Var "zf", NumF 3.))) ["x", F 10.])


// Problem 3

let to_float (v : value) : float =
    match v with
    | F v -> v
    | I v -> float(v)

printfn "Expected F 5.5, got %A" (to_float (F 5.5))
printfn "Expected F 5.0, got %A" (to_float (I 5))
printfn "Expected F -11.0, got %A" (to_float (I -11))
printfn "Expected F -11.0, got %A" (to_float (F -11.0))

// Problem 4

let to_float_expr (e : expr) : expr =

    Match(e, (fun e -> match e with | Var s -> s)e, IntToFloat(e), (fun e -> match e with | Var s -> s)e, e)
    


let plus_expr (e1 : expr, e2 : expr) : expr =

//    match Match(e1, (fun e1 -> match e1 with | Var s -> s)e1, IntToFloat(e1), (fun e1 -> match e1 with | Var s -> s)e1, e1) with
//    | Match _ -> match Match(e2, (fun e2 -> match e2 with | Var s -> s)e2, IntToFloat(e2), (fun e2 -> match e2 with | Var s -> s)e2, e2) with
//        | Match _ -> Plus(e1, e2)
//        | NumF _ -> Plus(to_float_expr(e1), e2)
//        | _ -> failwith "inner"
//    | NumF _ -> Plus(to_float_expr(e1), to_float_expr(e2))
//    | _ -> failwith "outer"

let times_expr (e1 : expr, e2 : expr) : expr =
    Times(to_float_expr(e1), to_float_expr(e2))

printfn "Expected F 4.0, got %A" (eval (to_float_expr (Var "x")) ["x", I 4])

printfn "Expected F 4.4, got %A" (eval (to_float_expr (Var "x")) ["x", F 4.4])


printfn "Expected I 13, got %A" (eval (plus_expr (Var "x", Var "y")) ["x", I 6; "y", I 7])

printfn "Expected F 13.1, got %A" (eval (plus_expr (Var "x", Var "y")) ["x", F 6.1; "y", I 7])

printfn "Expected F 13.2, got %A" (eval (plus_expr (Var "x", Var "y")) ["x", I 6; "y", F 7.2])

printfn "Expected F 13.3, got %A" (eval (plus_expr (Var "x", Var "y")) ["x", F 6.1; "y", F 7.2])

printfn "Expected I 42, got %A" (eval (times_expr (Var "x", Var "y")) ["x", I 6; "y", I 7])
printfn "Expected F 42.7, got %A" (eval (times_expr (Var "x", Var "y")) ["x", F 6.1; "y", I 7])

printfn "Expected F 43.2, got %A" (eval (times_expr (Var "x", Var "y")) ["x", I 6; "y", F 7.2])
// val it : value = F 43.2
// > eval (times_expr (Var "x", Var "y")) ["x", F 6.1; "y", F 7.2];;
// val it : value = F 43.92

// Problem 5

let rec add_matches (e : iexpr) : expr = failwith "to implement"


// Problem 6

let rec infer (e : expr) (tyenv : tyenvir) : typ =
    failwith "to implement"


// Problem 7

let add_casts (e : iexpr) (tyenv : tyenvir) : expr =
    failwith "to implement"


// Problem 8

// ANSWER 8 HERE:


// Problem 9

let rlower (inss : rcode) : rcode = failwith "to implement"