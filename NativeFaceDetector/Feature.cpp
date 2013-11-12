#include "pch.h"
#include "Feature.h"

#include <cmath>
#include <algorithm>

using namespace NativeFaceDetector;

Feature::Feature(float threshold, float leftVal, int leftNode, float rightVal, int rightNode, Size size)
	: m_size(size), m_threshold(threshold), m_leftVal(leftVal), m_rightVal(rightVal),
	m_leftNode(leftNode), m_rightNode(rightNode), m_numRects(0)
{
}

static inline float weightedSum(const RectFeature& feature, const Matrix &grayImage, int i, int j)
{
	const int top    = j + feature.scaledTop();
	const int bottom = j + feature.scaledBottom();
	const int left   = i + feature.scaledLeft();
	const int right  = i + feature.scaledRight();

	return feature.weight() * (grayImage(right, bottom) - grayImage(left, bottom) -
		                       grayImage(right, top)    + grayImage(left, top));
}

bool Feature::chooseLeft(const Matrix &grayImage, const Matrix &squares, int i, int j) const
{
	const int bottom = j + m_height;
	const int right  = i + m_width;

	const int total        = grayImage(right, bottom) + grayImage(i, j)
		                   - grayImage(i, bottom)     - grayImage(right, j);
	const int squaredTotal = squares(right, bottom) + squares(i, j)
		                   - squares(i, bottom)     - squares(right, j);

	const float mean       = total * m_invArea;
	const float variance   = squaredTotal * m_invArea - mean * mean;
	const float normalizer = (variance > 1.0f) ? sqrt(variance) : 1.0f;

	float rectSum = weightedSum(m_rects[0], grayImage, i, j);
	if (m_numRects > 1)
		rectSum += weightedSum(m_rects[1], grayImage, i, j);
	if (m_numRects > 2)
		rectSum += weightedSum(m_rects[2], grayImage, i, j);

	return rectSum < m_scaledThreshold * normalizer;
}