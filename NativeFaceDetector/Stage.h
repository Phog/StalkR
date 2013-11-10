#pragma once

#include "Matrix.h"
#include "Tree.h"

#include <vector>

namespace NativeFaceDetector
{

	class Stage
	{
		std::vector<Tree> m_trees;
		float             m_threshold;
	public:
		Stage(float threshold);

        void addTree(const Tree &t) { m_trees.push_back(t); }
		void setScale(float scale)
		{
			for (std::vector<Tree>::iterator i = m_trees.begin(); i < m_trees.end(); ++i)
				i->setScale(scale);
		}

		bool pass(const Matrix &grayImage, const Matrix &squares, int i, int j) const
		{
			float sum = 0.0f;
			for (std::vector<Tree>::const_iterator it = m_trees.cbegin(); it < m_trees.cend(); ++it)
				sum += it->getVal(grayImage, squares, i, j);

			return sum > m_threshold;
		}
	};

}
