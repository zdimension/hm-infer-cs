using hm_infer_cs;

var exprs = new[]
{
    @"(error ""this returns the bottom type (forall a. a)"")",
    @"(lambda (x) (if (zero x) (error ""divide by zero"") (/ 1 x)))",
    @"(pair 6)",
    @"(lambda (f) (f 5))",
    @"(lambda (f) f)",
    @"(lambda (x y) x)",
    @"((lambda (x y) x) #t 6)",
    @"(let ([five 5]) (let ([g (lambda (f) f)]) (g five)))",
    @"(let ([f (lambda (x) x)]) (pair (f 4) (f #t)))",
    @"(lambda (f) (f f))",
    @"(let ([g (lambda (f) 5)]) (g g))",
    @"(lambda (x) (pair x x))",
    @"((lambda (g) (pair 9 g)) 5)",
    @"(cdr ((lambda (g) (pair 9 g)) 5))",
    @"(lambda (x y) (pair y x))",
    @"(lambda (g) (let ((f (lambda (x) g))) (pair (f 3) (f #t))))",
    @"(lambda (f) (lambda (g) (lambda (arg) (g (f arg)))))",
    @"(letrec ((factorial (lambda (n) (if (zero n) 1 (* n (factorial (- n 1))))))) (factorial 5))",
    @"(lambda (l) (letrec ((length (lambda (l) (if (null? l) 0 (+ 1 (length (tl l))))))) (length l)))",
    @"(tl (cons 5 nil))",
    @"(let ([x (just 123)])
        (maybe x (= 123)))",
    @"(let ([x nothing])
        (maybe x (= 123)))",
    @"(let ([x (left 5)])
        (either x (= 123) (lambda (bool) 456)))",
    @"(let* (
             [kons (lambda (a b) (lambda (f) (f a b)))]
             [kar (lambda (p) (p (lambda (a d) a)))]
             [kdr (lambda (p) (p (lambda (a d) d)))]
             [test (kons 8 #t)]
             )
        (pair (kar test) (kdr test)))",
    @"(let (
            [multiple (lambda (k) (lambda(x) (= (modulo x k) 0)))]
            [singleton =]
            [union (lambda (a b) (lambda (x) (or (a x) (b x))))]
            [in? (lambda (n ens) (ens n))]
            )
        (in? 1 (union (multiple 5) (singleton 2))))"
};

var evaluated = exprs
    .Select(SExpr.Parse)
    .Select(e => (e.ToString(), e.Analyze()))
    .ToList();
var mx = evaluated.Max(t => t.Item1.Length);

foreach (var (e, t) in evaluated)
{
    TypeVariable.ResetChar();
    Console.WriteLine($"{e.PadRight(mx)} => {t}");
}