
let v = Vector(InputN(), InputN());
Printl(v.mag());
v.x = InputN();
Printl(v.mag());

function Vector(X, Y)
{
	public let x = X;
	public let y = Y;
	public function mag() => Sqrt(x^2 + y^2);
	

	return this;
}

Vector << z = 9;

Vector << mult = function(M) {
	x *= M;
	y *= M;
}