
let v = Vector(InputN(), InputN());
Printl(v.mag());
v.mult(InputN());
Printl(v.mag());

function Vector(X, Y)
{
	public let x = X;
	public let y = Y;
	public function mag() => Sqrt(x^2 + y^2);
	

	return this;
}

Vector << mult = function(M) {
	x *= M;
	y *= M;
}