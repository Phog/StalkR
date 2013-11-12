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

bool Feature::chooseLeft(const Matrix &grayImage, const Matrix &squares, int i, int j) const
{
	const int bottom = i + m_height;
	const int right  = j + m_width;

	const int total        = grayImage(bottom, right) + grayImage(i, j)
		                   - grayImage(i, right)     - grayImage(bottom, j);
	const int squaredTotal = squares(bottom, right) + squares(i, j)
		                   - squares(i, right)     - squares(bottom, j);

	const float mean       = total * m_invArea;
	const float variance   = squaredTotal * m_invArea - mean * mean;
	const float normalizer = (variance > 1.0f) ? sqrt(variance) : 1.0f;

	float rectSum = m_rects[0].weightedSum(grayImage, i, j);
	if (m_numRects > 1)
		rectSum += m_rects[1].weightedSum(grayImage, i, j);
	if (m_numRects > 2)
		rectSum += m_rects[2].weightedSum(grayImage, i, j);

	return rectSum < m_scaledThreshold * normalizer;
}