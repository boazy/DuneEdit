#pragma once

#include "types.h"

#include <array>
#include <vector>
#include <functional>
#include <istream>
#include <boost/optional.hpp>

struct rgb
{
	rgb() : r(0), g(0), b(0), alpha(255)
	{}

	rgb(byte r, byte g, byte b) : r(r), g(g), b(b), alpha(255)
	{}

	rgb(byte r, byte g, byte b, byte alpha) : r(r), g(g), b(b), alpha(alpha)
	{}

	byte r;
	byte g;
	byte b;
	byte alpha;
};

struct palette
{
	std::array<rgb, 256> colors;
};

struct sprite
{
	uint32_t width;
	uint32_t height;

	std::vector<rgb> pixels;
};

class image_parser
{
public:
	image_parser();

	bool transparent() const { return transparent_; }
	void set_transparent_(bool val) { transparent_ = val; }

	void parse(std::istream& input, const std::function<void(const sprite&)>& sprite_reciever);

private:
	bool transparent_;
};